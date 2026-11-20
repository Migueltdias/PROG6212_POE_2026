using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Security;

public sealed class UserSession
{
    public required string SessionId { get; init; }
    public required int UserId { get; init; }
    public required string Email { get; init; }
    public required string Role { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    internal string RefreshTokenHash { get; set; } = string.Empty;

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && now < ExpiresAt;
}

public sealed record SessionGrant(UserSession Session, string RefreshToken);

public interface ISessionStore
{
    SessionGrant Create(int userId, string role, string email);
    UserSession? GetActive(string sessionId);
    UserSession? FindActiveByRefreshToken(string refreshToken);

    SessionGrant Renew(UserSession session, string role);

    void Touch(UserSession session);
    bool Revoke(string sessionId);
    int RevokeAllForUser(int userId, string? exceptSessionId = null);
}

public sealed class InMemorySessionStore(TimeProvider clock, IOptions<JwtSettings> settings) : ISessionStore
{
    private readonly ConcurrentDictionary<string, UserSession> _sessions = new();
    private readonly ConcurrentDictionary<string, string> _sessionIdByRefreshHash = new();
    private readonly object _gate = new();

    public SessionGrant Create(int userId, string role, string email)
    {
        var now = clock.GetUtcNow();
        RemoveInactive(now);

        var refreshToken = NewSecret();
        var session = new UserSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            Email = email,
            Role = role,
            CreatedAt = now,
            LastSeenAt = now,
            ExpiresAt = now.AddDays(settings.Value.SessionDays),
            RefreshTokenHash = Hash(refreshToken)
        };

        lock (_gate)
        {
            _sessions[session.SessionId] = session;
            _sessionIdByRefreshHash[session.RefreshTokenHash] = session.SessionId;
        }

        return new SessionGrant(session, refreshToken);
    }

    public UserSession? GetActive(string sessionId) =>
        _sessions.TryGetValue(sessionId, out var session) && session.IsActive(clock.GetUtcNow()) ? session : null;

    public UserSession? FindActiveByRefreshToken(string refreshToken) =>
        _sessionIdByRefreshHash.TryGetValue(Hash(refreshToken), out var sessionId) ? GetActive(sessionId) : null;

    public SessionGrant Renew(UserSession session, string role)
    {
        var refreshToken = NewSecret();
        lock (_gate)
        {
            _sessionIdByRefreshHash.TryRemove(session.RefreshTokenHash, out _);
            session.RefreshTokenHash = Hash(refreshToken);
            session.Role = role;
            session.LastSeenAt = clock.GetUtcNow();
            session.ExpiresAt = session.LastSeenAt.AddDays(settings.Value.SessionDays);
            _sessionIdByRefreshHash[session.RefreshTokenHash] = session.SessionId;
        }

        return new SessionGrant(session, refreshToken);
    }

    public void Touch(UserSession session) => session.LastSeenAt = clock.GetUtcNow();

    public bool Revoke(string sessionId)
    {
        lock (_gate)
        {
            if (!_sessions.TryGetValue(sessionId, out var session) || session.RevokedAt is not null)
            {
                return false;
            }

            session.RevokedAt = clock.GetUtcNow();
            _sessionIdByRefreshHash.TryRemove(session.RefreshTokenHash, out _);
            return true;
        }
    }

    public int RevokeAllForUser(int userId, string? exceptSessionId = null)
    {
        var ids = _sessions.Values
            .Where(s => s.UserId == userId && s.SessionId != exceptSessionId && s.RevokedAt is null)
            .Select(s => s.SessionId)
            .ToList();

        return ids.Count(Revoke);
    }

    private void RemoveInactive(DateTimeOffset now)
    {
        foreach (var session in _sessions.Values.Where(s => !s.IsActive(now)).ToList())
        {
            lock (_gate)
            {
                _sessions.TryRemove(session.SessionId, out _);
                _sessionIdByRefreshHash.TryRemove(session.RefreshTokenHash, out _);
            }
        }
    }

    private static string NewSecret() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(48));

    private static string Hash(string token) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
