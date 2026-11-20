using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Models;

namespace RaceDay.Api.Security;

public sealed record AccessToken(string Token, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user, string role, string sessionId);
}

public sealed class TokenService(IOptions<JwtSettings> options, TimeProvider clock) : ITokenService
{
    private static readonly TimeSpan AllowedClockSkew = TimeSpan.FromSeconds(30);
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateAccessToken(User user, string role, string sessionId)
    {
        var settings = options.Value;
        var now = clock.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(settings.AccessTokenMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(CreateSigningKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [RaceDayClaims.UserId] = user.UserId.ToString(CultureInfo.InvariantCulture),
                [RaceDayClaims.Email] = user.Email,
                [RaceDayClaims.Role] = role,
                [RaceDayClaims.SessionId] = sessionId,
                [RaceDayClaims.GivenName] = user.FirstName,
                [RaceDayClaims.FamilyName] = user.LastName,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N")
            }
        };

        return new AccessToken(_handler.CreateToken(descriptor), expires);
    }

    public static SymmetricSecurityKey CreateSigningKey(string signingKey) => new(Encoding.UTF8.GetBytes(signingKey));

    public static bool IsWithinLifetime(DateTime? notBefore, DateTime? expires, DateTime utcNow) =>
        expires.HasValue
        && expires.Value.ToUniversalTime() > utcNow - AllowedClockSkew
        && (!notBefore.HasValue || notBefore.Value.ToUniversalTime() <= utcNow + AllowedClockSkew);
}
