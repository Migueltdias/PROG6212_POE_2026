using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class AuthenticationTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private readonly HttpClient _anonymous = factory.CreateAnonymousClient();

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task Register_creates_an_account_with_the_chosen_role(string role)
    {
        var email = $"New.{role}.{Guid.NewGuid():N}@Example.co.za";

        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", HttpTestExtensions.NewRegistration(email, role.ToLowerInvariant()));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var profile = await response.ReadAsync<UserProfileDto>();
        Assert.Equal(role, profile.Role);
        Assert.Equal(email.ToLowerInvariant(), profile.Email);
        Assert.True(profile.UserId > 8, "New accounts are numbered after the eight seeded users.");
        Assert.Equal($"/api/users/{profile.UserId}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Register_stores_a_salted_hash_and_never_the_password()
    {
        var email = $"hash.check.{Guid.NewGuid():N}@example.co.za";
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", HttpTestExtensions.NewRegistration(email));
        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);

        var user = await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.Email == email));
        var hasher = factory.Services.GetRequiredService<IPasswordHasher<User>>();

        Assert.NotEqual(TestUsers.Password, user.PasswordHash);
        Assert.DoesNotContain(TestUsers.Password, user.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, TestUsers.Password));
    }

    [Fact]
    public async Task Register_with_an_email_that_is_already_used_returns_409()
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register",
            HttpTestExtensions.NewRegistration("AYESHA.Patel@gmail.com"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        Assert.Contains("already exists", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task Register_with_mismatched_passwords_returns_400()
    {
        var registration = new
        {
            firstName = "Mis", lastName = "Match", email = $"mismatch.{Guid.NewGuid():N}@example.co.za",
            password = "Strong@2026", confirmPassword = "Different@2026",
            dateOfBirth = "1990-01-01", gender = "M", role = "Participant"
        };

        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", registration);

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("confirmPassword", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Theory]
    [InlineData("Admin", "role")]
    [InlineData("", "role")]
    public async Task Register_with_a_role_other_than_Organiser_or_Participant_returns_400(string role, string field)
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register",
            HttpTestExtensions.NewRegistration($"badrole.{Guid.NewGuid():N}@example.co.za", role));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains(field, (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("NoDigitsHere")]
    public async Task Register_with_a_weak_password_returns_400(string password)
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register",
            HttpTestExtensions.NewRegistration($"weak.{Guid.NewGuid():N}@example.co.za", password: password));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("password", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task Register_an_Organiser_under_18_returns_400()
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register",
            HttpTestExtensions.NewRegistration($"young.org.{Guid.NewGuid():N}@example.co.za", "Organiser", new DateOnly(2010, 5, 5)));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("dateOfBirth", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Theory]
    [InlineData(TestUsers.Thandeka, "Organiser")]
    [InlineData(TestUsers.Ayesha, "Participant")]
    public async Task Login_with_valid_credentials_returns_a_token_and_the_users_role(string email, string role)
    {
        var login = await _anonymous.LoginAsync(email, TestUsers.Password);

        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.False(string.IsNullOrWhiteSpace(login.RefreshToken));
        Assert.Equal(role, login.User.Role);
        Assert.Equal(RaceDayApiFactory.Today.UtcDateTime.AddMinutes(60), login.ExpiresAt);

        var token = new JsonWebToken(login.Token);
        Assert.Equal(role, token.GetClaim("role").Value);
        Assert.Equal(login.User.UserId.ToString(), token.Subject);
        Assert.False(string.IsNullOrEmpty(token.GetClaim("sid").Value));
    }

    [Fact]
    public async Task Login_is_case_insensitive_on_the_email()
    {
        var login = await _anonymous.LoginAsync("SIPHO.Ndlovu@Gmail.com", TestUsers.Password);

        Assert.Equal(3, login.User.UserId);
    }

    [Fact]
    public async Task Login_with_a_wrong_password_or_unknown_email_returns_the_same_401()
    {
        var wrongPassword = await _anonymous.PostAsJsonAsync("/api/auth/login", new { email = TestUsers.Ayesha, password = "Wrong@2026" });
        var unknownEmail = await _anonymous.PostAsJsonAsync("/api/auth/login", new { email = "nobody@example.co.za", password = "Wrong@2026" });

        await wrongPassword.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        await unknownEmail.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        Assert.Equal((await wrongPassword.ReadProblemAsync()).Detail, (await unknownEmail.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task Login_with_missing_fields_returns_400()
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/login", new { email = "" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task A_newly_registered_user_can_log_in()
    {
        var (client, profile) = await factory.RegisterAndLoginAsync();

        var me = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");

        Assert.Equal(profile.UserId, me!.UserId);
    }

    [Fact]
    public async Task A_deactivated_account_cannot_log_in()
    {
        var (client, profile) = await factory.RegisterAndLoginAsync();
        await (await client.DeleteAsync("/api/users/me")).ShouldHaveStatusAsync(HttpStatusCode.NoContent);

        var response = await _anonymous.PostAsJsonAsync("/api/auth/login", new { email = profile.Email, password = TestUsers.Password });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        Assert.Contains("deactivated", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task A_protected_endpoint_without_a_token_returns_401()
    {
        var response = await _anonymous.GetAsync("/api/users/me");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        Assert.Equal("Bearer", response.Headers.WwwAuthenticate.Single().Scheme);
        Assert.Contains("logged in", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task A_malformed_token_returns_401()
    {
        var client = factory.CreateAnonymousClient();
        client.UseToken("not.a.jwt");

        var response = await client.GetAsync("/api/users/me");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task A_token_signed_with_another_key_is_rejected()
    {
        var forged = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "RaceDay.Api",
            Audience = "RaceDay.Clients",
            IssuedAt = RaceDayApiFactory.Today.UtcDateTime,
            NotBefore = RaceDayApiFactory.Today.UtcDateTime,
            Expires = RaceDayApiFactory.Today.UtcDateTime.AddHours(1),
            Claims = new Dictionary<string, object> { ["sub"] = "1", ["role"] = "Organiser", ["sid"] = "made-up" },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes("an-attacker-key-that-is-also-32-chars-long!")),
                SecurityAlgorithms.HmacSha256)
        });
        var client = factory.CreateAnonymousClient();
        client.UseToken(forged);

        var response = await client.GetAsync("/api/events/mine");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }
}
