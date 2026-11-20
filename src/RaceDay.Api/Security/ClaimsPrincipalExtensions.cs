using System.Security.Claims;

namespace RaceDay.Api.Security;

public static class RaceDayClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Role = "role";
    public const string SessionId = "sid";
    public const string GivenName = "given_name";
    public const string FamilyName = "family_name";
}

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        user.TryGetUserId() ?? throw new InvalidOperationException("The request has no authenticated user.");

    public static int? TryGetUserId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(RaceDayClaims.UserId), out var id) ? id : null;

    public static string? GetSessionId(this ClaimsPrincipal user) => user.FindFirstValue(RaceDayClaims.SessionId);

    public static string? GetRole(this ClaimsPrincipal user) => user.FindFirstValue(RaceDayClaims.Role);
}
