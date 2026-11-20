using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Unit;

public class TokenServiceTests
{
    private const string Key = "unit-test-signing-key-that-is-long-enough-for-hs256";
    private readonly FixedTimeProvider _clock = new(RaceDayApiFactory.Today);
    private readonly TokenService _tokens;

    public TokenServiceTests() =>
        _tokens = new TokenService(Options.Create(new JwtSettings { SigningKey = Key, AccessTokenMinutes = 30 }), _clock);

    private static readonly User Ayesha = new()
    {
        UserId = 5, Email = "ayesha.patel@gmail.com", FirstName = "Ayesha", LastName = "Patel"
    };

    [Fact]
    public async Task The_token_carries_the_user_role_and_session_and_is_signed()
    {
        var token = _tokens.CreateAccessToken(Ayesha, "Participant", "session-123");

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, Parameters(Key));

        Assert.True(result.IsValid, result.Exception?.Message);
        Assert.Equal("5", result.Claims["sub"]);
        Assert.Equal("Participant", result.Claims["role"]);
        Assert.Equal("session-123", result.Claims["sid"]);
        Assert.Equal("ayesha.patel@gmail.com", result.Claims["email"]);
    }

    [Fact]
    public void The_token_expires_after_the_configured_minutes()
    {
        var token = _tokens.CreateAccessToken(Ayesha, "Participant", "session-123");

        Assert.Equal(RaceDayApiFactory.Today.UtcDateTime.AddMinutes(30), token.ExpiresAt);
    }

    [Fact]
    public async Task A_token_does_not_validate_with_a_different_key()
    {
        var token = _tokens.CreateAccessToken(Ayesha, "Participant", "session-123");

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Token, Parameters("a-completely-different-key-of-sufficient-length"));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(29, true)]
    [InlineData(31, false)]
    public void Lifetime_is_checked_against_the_clock(int minutesLater, bool expected)
    {
        var issued = RaceDayApiFactory.Today.UtcDateTime;

        var valid = TokenService.IsWithinLifetime(issued, issued.AddMinutes(30), issued.AddMinutes(minutesLater));

        Assert.Equal(expected, valid);
    }

    private TokenValidationParameters Parameters(string key) => new()
    {
        ValidIssuer = "RaceDay.Api",
        ValidAudience = "RaceDay.Clients",
        IssuerSigningKey = TokenService.CreateSigningKey(key),
        LifetimeValidator = (notBefore, expires, _, _) =>
            TokenService.IsWithinLifetime(notBefore, expires, _clock.GetUtcNow().UtcDateTime)
    };
}
