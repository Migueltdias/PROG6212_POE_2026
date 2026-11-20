using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Session;

public sealed class SessionUser
{
    public int UserId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime SessionExpiresAt { get; set; }

    [JsonIgnore]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [JsonIgnore]
    public string Initials => string.Concat(new[] { FirstName, LastName }
        .Where(n => !string.IsNullOrWhiteSpace(n))
        .Select(n => char.ToUpperInvariant(n.Trim()[0])));

    [JsonIgnore]
    public bool IsOrganiser => Role == Roles.Organiser;

    [JsonIgnore]
    public bool IsParticipant => Role == Roles.Participant;

    public static SessionUser From(LoginResponse login) => new()
    {
        UserId = login.User.UserId,
        Role = login.User.Role,
        Email = login.User.Email,
        FirstName = login.User.FirstName,
        LastName = login.User.LastName,
        ProfilePictureUrl = login.User.ProfilePictureUrl,
        AccessToken = login.Token,
        AccessTokenExpiresAt = login.ExpiresAt,
        RefreshToken = login.RefreshToken,
        SessionExpiresAt = login.SessionExpiresAt
    };

    public void Apply(TokenResponse tokens)
    {
        AccessToken = tokens.Token;
        AccessTokenExpiresAt = tokens.ExpiresAt;
        RefreshToken = tokens.RefreshToken;
        SessionExpiresAt = tokens.SessionExpiresAt;
    }

    public void Apply(UserProfile profile)
    {
        FirstName = profile.FirstName;
        LastName = profile.LastName;
        ProfilePictureUrl = profile.ProfilePictureUrl;
    }
}

public static class SessionKeys
{
    public const string UserId = "RaceDay.UserId";
    public const string Role = "RaceDay.Role";
    public const string User = "RaceDay.User";
}

public static class SessionUserExtensions
{
    public static SessionUser? GetUser(this ISession session)
    {
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);
        var json = session.GetString(SessionKeys.User);
        if (userId is null || string.IsNullOrEmpty(role) || string.IsNullOrEmpty(json))
        {
            return null;
        }

        SessionUser? user;
        try
        {
            user = JsonSerializer.Deserialize<SessionUser>(json);
        }
        catch (JsonException)
        {
            return null;
        }

        return user is not null && user.UserId == userId && user.Role == role ? user : null;
    }

    public static void SetUser(this ISession session, SessionUser user)
    {
        session.SetInt32(SessionKeys.UserId, user.UserId);
        session.SetString(SessionKeys.Role, user.Role);
        session.SetString(SessionKeys.User, JsonSerializer.Serialize(user));
    }

    public static SessionUser? GetSessionUser(this HttpContext? context) => context?.TryGetSession()?.GetUser();

    public static ISession? TryGetSession(this HttpContext context) => context.Features.Get<ISessionFeature>()?.Session;
}
