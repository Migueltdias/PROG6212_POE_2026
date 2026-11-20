using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Tests.Infrastructure;

public static class HttpTestExtensions
{
    public static void UseToken(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    public static async Task<LoginResponse> LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return await response.ReadAsync<LoginResponse>();
    }

    public static async Task<(HttpClient Client, UserProfileDto Profile)> RegisterAndLoginAsync(
        this RaceDayApiFactory factory, string role = "Participant", DateOnly? dateOfBirth = null, string gender = "F")
    {
        var email = $"{role.ToLowerInvariant()}.{Guid.NewGuid():N}@example.co.za";
        var anonymous = factory.CreateAnonymousClient();
        var response = await anonymous.PostAsJsonAsync("/api/auth/register", NewRegistration(email, role, dateOfBirth, gender));
        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var profile = await response.ReadAsync<UserProfileDto>();

        return (await factory.CreateClientForAsync(email, TestUsers.Password), profile);
    }

    public static object NewRegistration(string email, string role = "Participant", DateOnly? dateOfBirth = null,
        string gender = "F", string password = TestUsers.Password) => new
    {
        firstName = "Test",
        lastName = "Runner",
        email,
        password,
        confirmPassword = password,
        phoneNumber = "0821112222",
        dateOfBirth = (dateOfBirth ?? new DateOnly(1995, 4, 12)).ToString("yyyy-MM-dd"),
        gender,
        role
    };

    public static Task<HttpResponseMessage> SendJsonAsync(this HttpClient client, HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return client.SendAsync(request);
    }

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>())
        ?? throw new InvalidOperationException($"The response body could not be read as {typeof(T).Name}.");

    public static Task<ProblemDetails> ReadProblemAsync(this HttpResponseMessage response) => response.ReadAsync<ProblemDetails>();

    public static Task<ValidationProblemDetails> ReadValidationProblemAsync(this HttpResponseMessage response) =>
        response.ReadAsync<ValidationProblemDetails>();

    public static async Task ShouldHaveStatusAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected {(int)expected} {expected} from {response.RequestMessage?.Method} " +
                        $"{response.RequestMessage?.RequestUri?.PathAndQuery} but got {(int)response.StatusCode} " +
                        $"{response.StatusCode}: {body}");
        }
    }
}
