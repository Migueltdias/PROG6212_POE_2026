using Microsoft.AspNetCore.Http;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Session;

namespace RaceDay.Web.Tests.Unit;

public class SessionUserTests
{
    private static SessionUser Ayesha() => SessionUser.From(new LoginResponse
    {
        Token = "access-token",
        ExpiresAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc),
        RefreshToken = "refresh-token",
        SessionExpiresAt = new DateTime(2026, 10, 8, 8, 0, 0, DateTimeKind.Utc),
        User = new UserProfile { UserId = 5, Role = Roles.Participant, FirstName = "Ayesha", LastName = "Patel", Email = "ayesha.patel@gmail.com" }
    });

    [Fact]
    public void The_user_id_and_role_are_stored_under_their_own_session_keys()
    {
        var session = new MemorySession();

        session.SetUser(Ayesha());

        Assert.Equal(5, session.GetInt32(SessionKeys.UserId));
        Assert.Equal(Roles.Participant, session.GetString(SessionKeys.Role));
    }

    [Fact]
    public void The_whole_user_round_trips_through_the_session()
    {
        var session = new MemorySession();
        session.SetUser(Ayesha());

        var user = session.GetUser();

        Assert.NotNull(user);
        Assert.Equal("Ayesha Patel", user.FullName);
        Assert.Equal("AP", user.Initials);
        Assert.True(user.IsParticipant);
        Assert.False(user.IsOrganiser);
        Assert.Equal("access-token", user.AccessToken);
        Assert.Equal("refresh-token", user.RefreshToken);
    }

    [Fact]
    public void An_empty_session_has_no_user()
    {
        Assert.Null(new MemorySession().GetUser());
    }

    [Fact]
    public void A_record_that_disagrees_with_the_role_key_is_ignored()
    {
        var session = new MemorySession();
        session.SetUser(Ayesha());

        session.SetString(SessionKeys.Role, Roles.Organiser);

        Assert.Null(session.GetUser());
    }

    [Fact]
    public void A_corrupt_record_is_ignored_rather_than_crashing()
    {
        var session = new MemorySession();
        session.SetUser(Ayesha());

        session.SetString(SessionKeys.User, "{not json");

        Assert.Null(session.GetUser());
    }

    [Fact]
    public void Renewed_tokens_and_profile_changes_are_applied_to_the_user()
    {
        var user = Ayesha();

        user.Apply(new TokenResponse { Token = "new-access", RefreshToken = "new-refresh", ExpiresAt = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc) });
        user.Apply(new UserProfile { FirstName = "Ayesha", LastName = "Patel-Naidoo", ProfilePictureUrl = "https://x/me.png" });

        Assert.Equal("new-access", user.AccessToken);
        Assert.Equal("new-refresh", user.RefreshToken);
        Assert.Equal("Ayesha Patel-Naidoo", user.FullName);
        Assert.Equal("https://x/me.png", user.ProfilePictureUrl);
        Assert.Equal(5, user.UserId);
    }

    [Theory]
    [InlineData(Roles.Organiser, "Index", "Dashboard", "Organiser")]
    [InlineData(Roles.Participant, "Index", "Events", "Participant")]
    public void Each_role_has_its_own_home_page(string role, string action, string controller, string area)
    {
        Assert.Equal((action, controller, area), RoleHome.For(role));
    }

    private sealed class MemorySession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public bool IsAvailable => true;
        public string Id { get; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys => _values.Keys;

        public void Clear() => _values.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _values.Remove(key);
        public void Set(string key, byte[] value) => _values[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _values.TryGetValue(key, out value!);
    }
}
