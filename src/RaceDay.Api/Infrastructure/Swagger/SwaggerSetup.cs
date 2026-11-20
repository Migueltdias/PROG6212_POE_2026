using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace RaceDay.Api.Infrastructure.Swagger;

public static class SwaggerSetup
{
    public const string BearerScheme = "Bearer";

    public static readonly string[] SectionOrder =
    [
        "Authentication", "User Profile", "Events", "Event Categories",
        "Route Waypoints", "Enrolments", "Results", "System"
    ];

    private static readonly string[] MethodOrder = ["GET", "POST", "PUT", "PATCH", "DELETE"];

    private const string ApiDescription = """
        The RESTful API behind **RaceDay**, built to the Part 1 endpoint plan.

        **How to call a protected endpoint**
        1. `POST /api/auth/login` with one of the demo accounts below (password `RaceDay@2026`).
        2. Copy the `token` from the response, click **Authorize** at the top right and paste it.
        3. Every request now carries the token. `POST /api/auth/logout` ends the session.

        | Demo account | Role |
        |---|---|
        | thandeka.mokoena@raceday.co.za | Organiser (owns events 1 and 3) |
        | pieter.vanwyk@raceday.co.za | Organiser (owns event 2) |
        | ayesha.patel@gmail.com | Participant |
        | sipho.ndlovu@gmail.com | Participant |

        Each endpoint starts with the role it requires. Errors are returned as RFC 7807 problem details.
        """;

    public static IServiceCollection AddRaceDaySwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "RaceDay API",
                Version = "v1",
                Description = ApiDescription
            });

            var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(SwaggerSetup).Assembly.GetName().Name}.xml");
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            options.AddSecurityDefinition(BearerScheme, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Paste the token returned by POST /api/auth/login. Do not type the word Bearer."
            });
            options.OperationFilter<AuthorizationOperationFilter>();

            options.MapType<DateOnly>(() => new OpenApiSchema { Type = "string", Format = "date", Example = new OpenApiString("2026-11-01") });
            options.MapType<DateOnly?>(() => new OpenApiSchema { Type = "string", Format = "date", Nullable = true, Example = new OpenApiString("2026-11-01") });
            options.MapType<TimeOnly>(() => new OpenApiSchema { Type = "string", Format = "time", Example = new OpenApiString("06:00:00") });
            options.MapType<TimeOnly?>(() => new OpenApiSchema { Type = "string", Format = "time", Nullable = true, Example = new OpenApiString("06:00:00") });

            options.TagActionsBy(api => [TagOf(api)]);
            options.OrderActionsBy(api =>
            {
                var section = Array.IndexOf(SectionOrder, TagOf(api));
                var method = Array.IndexOf(MethodOrder, api.HttpMethod?.ToUpperInvariant());
                return $"{section:D2}_{api.RelativePath}_{method}";
            });

            options.SupportNonNullableReferenceTypes();
        });

        return services;
    }

    public static WebApplication UseRaceDaySwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
            options.DocumentTitle = "RaceDay API";
            options.DocExpansion(DocExpansion.List);
            options.DisplayRequestDuration();
            options.EnablePersistAuthorization();
        });

        app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().ExcludeFromDescription();
        return app;
    }

    private static string TagOf(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription api) =>
        api.ActionDescriptor.EndpointMetadata.OfType<TagsAttribute>().FirstOrDefault()?.Tags.FirstOrDefault()
        ?? api.ActionDescriptor.RouteValues["controller"]
        ?? "Other";
}
