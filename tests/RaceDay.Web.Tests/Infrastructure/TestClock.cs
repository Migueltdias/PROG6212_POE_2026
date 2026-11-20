namespace RaceDay.Web.Tests.Infrastructure;

public sealed class TestClock : TimeProvider
{
    public static readonly TestClock Instance = new();

    public override DateTimeOffset GetUtcNow() => new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);
}
