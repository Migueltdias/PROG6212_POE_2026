using Microsoft.Extensions.Options;
using RaceDay.Api.Security;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Unit;

public class SessionStoreTests
{
    private readonly FixedTimeProvider _clock = new(RaceDayApiFactory.Today);
    private readonly InMemorySessionStore _store;

    public SessionStoreTests() =>
        _store = new InMemorySessionStore(_clock, Options.Create(new JwtSettings { SessionDays = 7 }));

    [Fact]
    public void A_new_session_remembers_the_user_and_role()
    {
        var grant = _store.Create(5, "Participant", "ayesha.patel@gmail.com");

        var session = _store.GetActive(grant.Session.SessionId);

        Assert.NotNull(session);
        Assert.Equal(5, session.UserId);
        Assert.Equal("Participant", session.Role);
        Assert.Equal(RaceDayApiFactory.Today.AddDays(7), session.ExpiresAt);
        Assert.Same(session, _store.FindActiveByRefreshToken(grant.RefreshToken));
    }

    [Fact]
    public void The_refresh_token_itself_is_not_stored()
    {
        var grant = _store.Create(5, "Participant", "ayesha.patel@gmail.com");

        Assert.NotEqual(grant.RefreshToken, grant.Session.RefreshTokenHash);
        Assert.NotEmpty(grant.Session.RefreshTokenHash);
    }

    [Fact]
    public void A_revoked_session_is_no_longer_active()
    {
        var grant = _store.Create(5, "Participant", "ayesha.patel@gmail.com");

        Assert.True(_store.Revoke(grant.Session.SessionId));

        Assert.Null(_store.GetActive(grant.Session.SessionId));
        Assert.Null(_store.FindActiveByRefreshToken(grant.RefreshToken));
        Assert.False(_store.Revoke(grant.Session.SessionId));
    }

    [Fact]
    public void Renewing_rotates_the_refresh_token_and_extends_the_session()
    {
        var grant = _store.Create(5, "Participant", "ayesha.patel@gmail.com");
        _clock.Advance(TimeSpan.FromDays(3));

        var renewed = _store.Renew(grant.Session, "Participant");

        Assert.NotEqual(grant.RefreshToken, renewed.RefreshToken);
        Assert.Null(_store.FindActiveByRefreshToken(grant.RefreshToken));
        Assert.NotNull(_store.FindActiveByRefreshToken(renewed.RefreshToken));
        Assert.Equal(_clock.GetUtcNow().AddDays(7), renewed.Session.ExpiresAt);
    }

    [Fact]
    public void A_session_ends_when_its_lifetime_runs_out()
    {
        var grant = _store.Create(5, "Participant", "ayesha.patel@gmail.com");

        _clock.Advance(TimeSpan.FromDays(7));

        Assert.Null(_store.GetActive(grant.Session.SessionId));
    }

    [Fact]
    public void Revoking_all_of_a_users_sessions_can_keep_the_current_one()
    {
        var current = _store.Create(5, "Participant", "ayesha.patel@gmail.com");
        var other = _store.Create(5, "Participant", "ayesha.patel@gmail.com");
        var someoneElse = _store.Create(3, "Participant", "sipho.ndlovu@gmail.com");

        var revoked = _store.RevokeAllForUser(5, exceptSessionId: current.Session.SessionId);

        Assert.Equal(1, revoked);
        Assert.NotNull(_store.GetActive(current.Session.SessionId));
        Assert.Null(_store.GetActive(other.Session.SessionId));
        Assert.NotNull(_store.GetActive(someoneElse.Session.SessionId));
    }
}
