using System.Net;
using System.Text;
using System.Text.Json;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class RoleAccessTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private static readonly string EventBody = Json(TestData.NewEvent("Hijack Attempt"));
    private static readonly string CategoryBody = Json(TestData.NewCategory());
    private static readonly string WaypointBody = Json(TestData.NewWaypoint(20, 1m));
    private static readonly string EntryBody = Json(TestData.Entry());
    private static readonly string PaymentBody = Json(new { amountPaid = 890m, paymentReference = "PAY-X" });
    private static readonly string ResultBody = Json(new { finishTime = "11:00:00", overallPosition = 1, resultStatus = "Finished" });
    private static readonly string BulkBody = Json(new { results = new[] { new { raceNumber = "CM1042", resultStatus = "DNS" } } });
    private static readonly string StatusBody = Json(new { status = "Cancelled" });

    public static TheoryData<string, string> ProtectedEndpoints => new()
    {
        { "POST", "/api/auth/logout" },
        { "GET", "/api/users/me" },
        { "PUT", "/api/users/me" },
        { "PUT", "/api/users/me/password" },
        { "DELETE", "/api/users/me" },
        { "GET", "/api/users" },
        { "GET", "/api/users/1" },
        { "POST", "/api/events" },
        { "PUT", "/api/events/2" },
        { "PATCH", "/api/events/2/status" },
        { "DELETE", "/api/events/2" },
        { "GET", "/api/events/mine" },
        { "GET", "/api/events/2/enrolments" },
        { "POST", "/api/events/2/categories" },
        { "PUT", "/api/categories/3" },
        { "DELETE", "/api/categories/3" },
        { "POST", "/api/categories/3/waypoints" },
        { "PUT", "/api/waypoints/1" },
        { "DELETE", "/api/waypoints/1" },
        { "POST", "/api/categories/3/enrolments" },
        { "GET", "/api/enrolments/mine" },
        { "GET", "/api/enrolments/1" },
        { "PUT", "/api/enrolments/1" },
        { "PATCH", "/api/enrolments/1/payment" },
        { "DELETE", "/api/enrolments/1" },
        { "GET", "/api/categories/3/enrolments" },
        { "POST", "/api/enrolments/1/result" },
        { "POST", "/api/categories/1/results/bulk" },
        { "PUT", "/api/results/1" },
        { "DELETE", "/api/results/1" },
        { "GET", "/api/results/mine" }
    };

    public static TheoryData<string, string> OrganiserOnlyEndpoints => new()
    {
        { "GET", "/api/users" },
        { "GET", "/api/users/1" },
        { "POST", "/api/events" },
        { "PUT", "/api/events/2" },
        { "PATCH", "/api/events/2/status" },
        { "DELETE", "/api/events/2" },
        { "GET", "/api/events/mine" },
        { "GET", "/api/events/2/enrolments" },
        { "POST", "/api/events/2/categories" },
        { "PUT", "/api/categories/3" },
        { "DELETE", "/api/categories/3" },
        { "POST", "/api/categories/3/waypoints" },
        { "PUT", "/api/waypoints/1" },
        { "DELETE", "/api/waypoints/1" },
        { "GET", "/api/categories/3/enrolments" },
        { "POST", "/api/enrolments/1/result" },
        { "POST", "/api/categories/1/results/bulk" },
        { "PUT", "/api/results/1" },
        { "DELETE", "/api/results/1" }
    };

    public static TheoryData<string, string> ParticipantOnlyEndpoints => new()
    {
        { "POST", "/api/categories/3/enrolments" },
        { "GET", "/api/enrolments/mine" },
        { "PUT", "/api/enrolments/1" },
        { "PATCH", "/api/enrolments/1/payment" },
        { "DELETE", "/api/enrolments/1" },
        { "GET", "/api/results/mine" }
    };

    public static TheoryData<string> PublicEndpoints => new()
    {
        "/api/events",
        "/api/events/2",
        "/api/events/1/results",
        "/api/events/2/weather",
        "/api/events/2/categories",
        "/api/categories/3",
        "/api/categories/1/waypoints",
        "/api/categories/1/results",
        "/api/results/1",
        "/api/health"
    };

    public static TheoryData<string, string, string?> AnotherOrganisersRecords => new()
    {
        { "PUT", "/api/events/3", EventBody },
        { "PATCH", "/api/events/3/status", StatusBody },
        { "DELETE", "/api/events/3", null },
        { "GET", "/api/events/3/enrolments", null },
        { "POST", "/api/events/3/categories", CategoryBody },
        { "PUT", "/api/categories/6", CategoryBody },
        { "DELETE", "/api/categories/6", null },
        { "POST", "/api/categories/6/waypoints", WaypointBody },
        { "PUT", "/api/waypoints/10", WaypointBody },
        { "DELETE", "/api/waypoints/10", null },
        { "GET", "/api/categories/6/enrolments", null },
        { "GET", "/api/enrolments/1", null },
        { "POST", "/api/enrolments/1/result", ResultBody },
        { "POST", "/api/categories/1/results/bulk", BulkBody },
        { "PUT", "/api/results/1", ResultBody },
        { "DELETE", "/api/results/1", null }
    };

    public static TheoryData<string, string, string?> AnotherParticipantsEntry => new()
    {
        { "GET", "/api/enrolments/12", null },
        { "PUT", "/api/enrolments/12", EntryBody },
        { "PATCH", "/api/enrolments/12/payment", PaymentBody },
        { "DELETE", "/api/enrolments/12", null }
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Unauthenticated_requests_to_non_public_endpoints_return_401(string method, string url)
    {
        var response = await Send(factory.CreateAnonymousClient(), method, url, "{}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(OrganiserOnlyEndpoints))]
    public async Task A_Participant_is_refused_Organiser_functionality(string method, string url)
    {
        var participant = await factory.CreateCachedClientForAsync(TestUsers.Sipho);

        var response = await Send(participant, method, url, "{}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
        Assert.Contains("Participant", (await response.ReadProblemAsync()).Detail);
    }

    [Theory]
    [MemberData(nameof(ParticipantOnlyEndpoints))]
    public async Task An_Organiser_is_refused_Participant_functionality(string method, string url)
    {
        var organiser = await factory.CreateCachedClientForAsync(TestUsers.Thandeka);

        var response = await Send(organiser, method, url, "{}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
        Assert.Contains("Organiser", (await response.ReadProblemAsync()).Detail);
    }

    [Theory]
    [MemberData(nameof(AnotherOrganisersRecords))]
    public async Task An_Organiser_cannot_manage_another_Organisers_event(string method, string url, string? body)
    {
        var otherOrganiser = await factory.CreateCachedClientForAsync(TestUsers.Pieter);

        var response = await Send(otherOrganiser, method, url, body);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(AnotherParticipantsEntry))]
    public async Task A_Participant_cannot_see_or_change_another_Participants_entry(string method, string url, string? body)
    {
        var otherParticipant = await factory.CreateCachedClientForAsync(TestUsers.Sipho);

        var response = await Send(otherParticipant, method, url, body);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }

    [Theory]
    [MemberData(nameof(PublicEndpoints))]
    public async Task Public_endpoints_need_no_token(string url)
    {
        var response = await factory.CreateAnonymousClient().GetAsync(url);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(TestUsers.Thandeka)]
    [InlineData(TestUsers.Ayesha)]
    public async Task Both_roles_can_view_events_and_categories(string email)
    {
        var client = await factory.CreateCachedClientForAsync(email);

        await (await client.GetAsync("/api/events")).ShouldHaveStatusAsync(HttpStatusCode.OK);
        await (await client.GetAsync("/api/events/3")).ShouldHaveStatusAsync(HttpStatusCode.OK);
        await (await client.GetAsync("/api/events/3/categories")).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string method, string url, string? body)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        return client.SendAsync(request);
    }

    private static string Json(object value) => JsonSerializer.Serialize(value);
}
