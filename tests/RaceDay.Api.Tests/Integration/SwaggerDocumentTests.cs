using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Infrastructure.Swagger;
using RaceDay.Api.Tests.Infrastructure;
using Swashbuckle.AspNetCore.Swagger;

namespace RaceDay.Api.Tests.Integration;

public class SwaggerDocumentTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private static readonly string[] PlannedEndpoints =
    [
        "POST /api/auth/register", "POST /api/auth/login", "POST /api/auth/refresh", "POST /api/auth/logout",
        "GET /api/users/me", "PUT /api/users/me", "PUT /api/users/me/password", "DELETE /api/users/me",
        "GET /api/users", "GET /api/users/{id}",
        "GET /api/events", "GET /api/events/{id}", "POST /api/events", "PUT /api/events/{id}",
        "PATCH /api/events/{id}/status", "DELETE /api/events/{id}", "GET /api/events/mine",
        "GET /api/events/{id}/enrolments", "GET /api/events/{id}/results", "GET /api/events/{id}/weather",
        "GET /api/events/{eventId}/categories", "GET /api/categories/{id}", "POST /api/events/{eventId}/categories",
        "PUT /api/categories/{id}", "DELETE /api/categories/{id}",
        "GET /api/categories/{id}/waypoints", "POST /api/categories/{id}/waypoints",
        "PUT /api/waypoints/{id}", "DELETE /api/waypoints/{id}",
        "POST /api/categories/{categoryId}/enrolments", "GET /api/enrolments/mine", "GET /api/enrolments/{id}",
        "PUT /api/enrolments/{id}", "PATCH /api/enrolments/{id}/payment", "DELETE /api/enrolments/{id}",
        "GET /api/categories/{id}/enrolments",
        "POST /api/enrolments/{id}/result", "POST /api/categories/{id}/results/bulk", "PUT /api/results/{id}",
        "DELETE /api/results/{id}", "GET /api/results/{id}", "GET /api/results/mine", "GET /api/categories/{id}/results",
        "GET /api/health"
    ];

    private static readonly string[] Part3ImageEndpoints =
    [
        "PUT /api/events/{id}/banner", "DELETE /api/events/{id}/banner",
        "PUT /api/users/me/profile-picture", "DELETE /api/users/me/profile-picture"
    ];

    private static readonly string[] PublicEndpoints =
    [
        "POST /api/auth/register", "POST /api/auth/login", "POST /api/auth/refresh",
        "GET /api/events", "GET /api/events/{id}", "GET /api/events/{id}/results", "GET /api/events/{id}/weather",
        "GET /api/events/{eventId}/categories", "GET /api/categories/{id}", "GET /api/categories/{id}/waypoints",
        "GET /api/results/{id}", "GET /api/categories/{id}/results", "GET /api/health"
    ];

    private Dictionary<string, OpenApiOperation> Operations()
    {
        var document = factory.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
        return document.Paths
            .SelectMany(path => path.Value.Operations.Select(op => ($"{op.Key.ToString().ToUpperInvariant()} {path.Key}", op.Value)))
            .ToDictionary(x => x.Item1, x => x.Item2);
    }

    [Fact]
    public void Swagger_lists_the_44_planned_endpoints_and_the_Part_3_image_endpoints()
    {
        var documented = Operations().Keys.Order().ToList();

        Assert.Equal(44, PlannedEndpoints.Length);
        Assert.Equal(PlannedEndpoints.Concat(Part3ImageEndpoints).Order(), documented);
    }

    [Fact]
    public void Image_uploads_are_documented_as_multipart_file_fields()
    {
        var operations = Operations();

        foreach (var name in Part3ImageEndpoints.Where(e => e.StartsWith("PUT ")))
        {
            var schema = operations[name].RequestBody.Content["multipart/form-data"].Schema;
            Assert.Contains(schema.Properties, p => p.Key.Equals("file", StringComparison.OrdinalIgnoreCase) && p.Value.Format == "binary");
        }
    }

    [Fact]
    public void Every_endpoint_describes_its_purpose_its_role_and_its_responses()
    {
        foreach (var (name, operation) in Operations())
        {
            Assert.False(string.IsNullOrWhiteSpace(operation.Summary), $"{name} has no summary.");
            Assert.StartsWith("**Role required:**", operation.Description);
            Assert.Contains(operation.Responses.Keys, code => code.StartsWith('2'));
            Assert.All(operation.Responses, r => Assert.False(string.IsNullOrWhiteSpace(r.Value.Description), $"{name} {r.Key} has no description."));
        }
    }

    [Fact]
    public void Endpoints_that_take_a_body_document_its_schema()
    {
        var withBodies = Operations().Where(o => o.Value.RequestBody is not null).ToList();

        Assert.NotEmpty(withBodies);
        Assert.All(withBodies, o =>
        {
            var mediaType = Part3ImageEndpoints.Contains(o.Key) ? "multipart/form-data" : "application/json";
            Assert.NotNull(o.Value.RequestBody.Content[mediaType].Schema);
        });
    }

    [Fact]
    public void Only_non_public_endpoints_ask_for_the_bearer_token()
    {
        foreach (var (name, operation) in Operations())
        {
            var needsToken = operation.Security.Any();
            Assert.True(needsToken != PublicEndpoints.Contains(name),
                $"{name} should {(PublicEndpoints.Contains(name) ? "not " : string.Empty)}require a token.");
            if (needsToken)
            {
                Assert.Contains("401", operation.Responses.Keys);
            }
        }
    }

    [Fact]
    public void Endpoints_are_grouped_by_the_sections_of_the_plan()
    {
        var tags = Operations().Values.Select(o => Assert.Single(o.Tags).Name).Distinct().Order();

        Assert.Equal(SwaggerSetup.SectionOrder.Order(), tags);
    }

    [Fact]
    public void Owner_only_endpoints_say_so_in_their_description()
    {
        var operations = Operations();

        Assert.Contains("Organiser (owner)", operations["PUT /api/events/{id}"].Description);
        Assert.Contains("Participant (own)", operations["DELETE /api/enrolments/{id}"].Description);
        Assert.Contains("None", operations["GET /api/events"].Description);
    }
}
