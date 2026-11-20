using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class SessionTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task The_session_carries_the_user_and_role_across_requests()
    {
        var client = await factory.CreateClientForAsync(TestUsers.Pieter);

        var me = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");
        var mine = await client.GetAsync("/api/events/mine");

        Assert.Equal("Organiser", me!.Role);
        await mine.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logging_out_ends_the_session_so_the_same_token_is_rejected()
    {
        var client = await factory.CreateClientForAsync(TestUsers.Johan);
        await (await client.GetAsync("/api/users/me")).ShouldHaveStatusAsync(HttpStatusCode.OK);

        await (await client.PostAsync("/api/auth/logout", null)).ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        var afterLogout = await client.GetAsync("/api/users/me");

        await afterLogout.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        Assert.Contains("session has ended", (await afterLogout.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task Logging_out_one_session_leaves_the_users_other_sessions_signed_in()
    {
        var laptop = await factory.CreateClientForAsync(TestUsers.Daniel);
        var phone = await factory.CreateClientForAsync(TestUsers.Daniel);

        await laptop.PostAsync("/api/auth/logout", null);

        await (await phone.GetAsync("/api/users/me")).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_without_a_token_returns_401()
    {
        var response = await factory.CreateAnonymousClient().PostAsync("/api/auth/logout", null);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_issues_a_working_token_and_the_used_refresh_token_stops_working()
    {
        var anonymous = factory.CreateAnonymousClient();
        var login = await anonymous.LoginAsync(TestUsers.Lerato, TestUsers.Password);

        var refreshed = await anonymous.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });
        await refreshed.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var tokens = await refreshed.ReadAsync<TokenResponse>();
        var reused = await anonymous.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        Assert.NotEqual(login.RefreshToken, tokens.RefreshToken);
        await reused.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);

        var client = factory.CreateAnonymousClient();
        client.UseToken(tokens.Token);
        await (await client.GetAsync("/api/users/me")).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_after_logout_is_rejected()
    {
        var anonymous = factory.CreateAnonymousClient();
        var login = await anonymous.LoginAsync(TestUsers.Nomsa, TestUsers.Password);
        var client = factory.CreateAnonymousClient();
        client.UseToken(login.Token);
        await client.PostAsync("/api/auth/logout", null);

        var response = await anonymous.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_with_an_unknown_token_returns_401()
    {
        var response = await factory.CreateAnonymousClient().PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "made-up" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_expired_access_token_is_rejected_but_the_session_can_be_refreshed()
    {
        await using var timeTravel = new RaceDayApiFactory();
        var anonymous = timeTravel.CreateAnonymousClient();
        var login = await anonymous.LoginAsync(TestUsers.Sipho, TestUsers.Password);
        var client = timeTravel.CreateAnonymousClient();
        client.UseToken(login.Token);

        timeTravel.Clock.Advance(TimeSpan.FromMinutes(61));
        var expired = await client.GetAsync("/api/users/me");
        var refreshed = await anonymous.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        await expired.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        Assert.Contains("expired", (await expired.ReadProblemAsync()).Detail);
        await refreshed.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_session_that_is_not_refreshed_within_its_lifetime_ends()
    {
        await using var timeTravel = new RaceDayApiFactory();
        var anonymous = timeTravel.CreateAnonymousClient();
        var login = await anonymous.LoginAsync(TestUsers.Ayesha, TestUsers.Password);

        timeTravel.Clock.Advance(TimeSpan.FromDays(8));
        var response = await anonymous.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = login.RefreshToken });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }
}
