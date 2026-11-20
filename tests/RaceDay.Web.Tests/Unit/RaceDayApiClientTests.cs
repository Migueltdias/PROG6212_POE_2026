using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Tests.Unit;

public class RaceDayApiClientTests
{
    private static (RaceDayApiClient Client, StubHandler Handler) ClientAnswering(Func<HttpRequestMessage, HttpResponseMessage> answer)
    {
        var handler = new StubHandler(answer);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") };
        return (new RaceDayApiClient(http, NullLogger<RaceDayApiClient>.Instance), handler);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/problem+json") };

    [Fact]
    public async Task A_successful_call_returns_the_deserialised_value()
    {
        var (client, handler) = ClientAnswering(_ => Json(HttpStatusCode.OK,
            """{"eventId":2,"eventName":"Soweto Marathon 2026","eventDate":"2026-11-01","startTime":"06:00:00","categories":[{"categoryId":4,"distanceKm":21.1,"entryFee":300.00}]}"""));

        var result = await client.GetEventAsync(2);

        Assert.True(result.Succeeded);
        Assert.Equal("Soweto Marathon 2026", result.Value!.EventName);
        Assert.Equal(new DateOnly(2026, 11, 1), result.Value.EventDate);
        Assert.Equal(new TimeOnly(6, 0), result.Value.StartTime);
        Assert.Equal(21.1m, Assert.Single(result.Value.Categories).DistanceKm);
        Assert.Equal("http://api.test/api/events/2", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task A_validation_problem_keeps_the_errors_per_field()
    {
        var (client, _) = ClientAnswering(_ => Json(HttpStatusCode.BadRequest,
            """{"title":"One or more validation errors occurred.","status":400,"errors":{"eventDate":["The event date cannot be in the past."],"city":["The City field is required."]}}"""));

        var result = await client.CreateEventAsync(new CreateEventRequest());

        Assert.False(result.Succeeded);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(["The event date cannot be in the past."], result.FieldErrors["eventDate"]);
        Assert.Null(result.Error);
        Assert.Equal("The event date cannot be in the past.", result.ErrorMessage);
    }

    [Fact]
    public async Task A_conflict_keeps_the_apis_explanation()
    {
        var (client, _) = ClientAnswering(_ => Json(HttpStatusCode.Conflict,
            """{"title":"Conflict","status":409,"detail":"This category is full (500 entries)."}"""));

        var result = await client.EnterAsync(4, new EnrolmentRequest());

        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Equal("This category is full (500 entries).", result.ErrorMessage);
    }

    [Fact]
    public async Task A_401_that_challenges_for_a_token_means_the_session_has_ended()
    {
        var (client, _) = ClientAnswering(_ =>
        {
            var response = Json(HttpStatusCode.Unauthorized, """{"title":"Unauthorized","status":401,"detail":"Your session has ended because you logged out."}""");
            response.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Bearer"));
            return response;
        });

        var error = await Assert.ThrowsAsync<ApiSessionExpiredException>(() => client.GetMyProfileAsync());

        Assert.Equal("Your session has ended because you logged out.", error.Message);
    }

    [Fact]
    public async Task A_401_without_a_challenge_is_an_ordinary_failure_such_as_a_wrong_password()
    {
        var (client, _) = ClientAnswering(_ => Json(HttpStatusCode.Unauthorized,
            """{"title":"Unauthorized","status":401,"detail":"The current password is incorrect."}"""));

        var result = await client.ChangePasswordAsync(new ChangePasswordRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("The current password is incorrect.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_goes_out_without_the_users_token()
    {
        var (client, handler) = ClientAnswering(_ => Json(HttpStatusCode.Unauthorized, """{"detail":"The email address or password is incorrect."}"""));

        var result = await client.LoginAsync(new LoginRequest { Email = "a@b.co.za", Password = "x" });

        Assert.False(result.Succeeded);
        Assert.True(handler.LastRequest!.Options.TryGetValue(ApiRequestOptions.Anonymous, out var anonymous) && anonymous);
    }

    [Fact]
    public async Task An_unreachable_api_becomes_ApiUnavailableException()
    {
        var (client, _) = ClientAnswering(_ => throw new HttpRequestException("Connection refused"));

        await Assert.ThrowsAsync<ApiUnavailableException>(() => client.BrowseEventsAsync(new EventQuery()));
    }

    [Fact]
    public async Task A_server_error_without_a_problem_body_still_produces_a_message()
    {
        var (client, _) = ClientAnswering(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>bad gateway</html>") });

        var result = await client.GetEventAsync(1);

        Assert.Equal(HttpStatusCode.BadGateway, result.StatusCode);
        Assert.Contains("502", result.ErrorMessage);
    }

    [Fact]
    public async Task Images_are_posted_as_multipart_in_a_file_field()
    {
        var (client, handler) = ClientAnswering(_ => Json(HttpStatusCode.OK, """{"eventId":9,"bannerImageUrl":"https://x/b.png"}"""));

        await client.UploadEventBannerAsync(9, new MemoryStream([1, 2, 3]), @"C:\photos\banner.png", "image/png");

        Assert.Equal(HttpMethod.Put, handler.LastRequest!.Method);
        Assert.Equal("multipart/form-data", handler.LastContentType);
        Assert.Contains("name=file", handler.LastBody);
        Assert.Contains("filename=banner.png", handler.LastBody);
    }

    [Fact]
    public void Query_strings_skip_empty_values_and_format_dates_the_way_the_api_expects()
    {
        var query = RaceDayApiClient.Query(("province", "KwaZulu-Natal"), ("search", " "), ("discipline", null),
            ("fromDate", new DateOnly(2026, 10, 1)), ("upcoming", true), ("page", 2));

        Assert.Equal("?province=KwaZulu-Natal&fromDate=2026-10-01&upcoming=true&page=2", query);
    }

    [Fact]
    public void Require_returns_the_value_or_throws_with_the_failed_result()
    {
        var ok = new ApiResult<string> { StatusCode = HttpStatusCode.OK, Value = "yes" };
        var missing = new ApiResult<string> { StatusCode = HttpStatusCode.NotFound, Error = "There is no event with id 9." };

        Assert.Equal("yes", ok.Require());
        var error = Assert.Throws<ApiRequestFailedException>(() => missing.Require());
        Assert.Equal(HttpStatusCode.NotFound, error.Result.StatusCode);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastContentType { get; private set; }
        public string LastBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastContentType = request.Content?.Headers.ContentType?.MediaType;
            LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return answer(request);
        }
    }
}
