using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RaceDay.Api.Infrastructure.Swagger;

public sealed class AuthorizationOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var isPublic = metadata.OfType<IAllowAnonymous>().Any();

        var roles = metadata.OfType<IAuthorizeData>()
            .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
            .SelectMany(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct()
            .ToList();

        var note = metadata.OfType<AccessNoteAttribute>().FirstOrDefault()?.Note
                   ?? (isPublic ? "None (public, no token needed)"
                       : roles.Count == 0 ? "Any signed-in user"
                       : string.Join(" or ", roles));

        operation.Description = $"**Role required:** {note}"
                                + (string.IsNullOrWhiteSpace(operation.Description) ? string.Empty : $"\n\n{operation.Description}");

        if (isPublic)
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SwaggerSetup.BearerScheme }
                }] = Array.Empty<string>()
            }
        ];

        operation.Responses.TryAdd("401", new OpenApiResponse
        {
            Description = "Unauthorized: no token, an expired or invalid token, or a session that has been logged out."
        });

        if (roles.Count > 0)
        {
            operation.Responses.TryAdd("403", new OpenApiResponse
            {
                Description = $"Forbidden: the caller is signed in but is not allowed here ({note})."
            });
        }
    }
}
