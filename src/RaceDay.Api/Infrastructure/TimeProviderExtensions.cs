namespace RaceDay.Api.Infrastructure;

public static class TimeProviderExtensions
{
    public static DateTime UtcNowSeconds(this TimeProvider clock)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        return new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
    }

    public static DateOnly Today(this TimeProvider clock) => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    public static int AgeOn(DateOnly dateOfBirth, DateOnly onDate)
    {
        var age = onDate.Year - dateOfBirth.Year;
        if (dateOfBirth > onDate.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}
