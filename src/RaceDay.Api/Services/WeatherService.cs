using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Services;

public interface IWeatherService
{
    Task<ServiceResult<WeatherSnapshotDto>> GetLatestAsync(int eventId, int? callerId, CancellationToken cancellationToken = default);
}

public sealed class WeatherService(RaceDayDbContext db) : IWeatherService
{
    public async Task<ServiceResult<WeatherSnapshotDto>> GetLatestAsync(int eventId, int? callerId, CancellationToken cancellationToken = default)
    {
        if (!await db.Events.Where(e => e.EventId == eventId).VisibleTo(callerId).AnyAsync(cancellationToken))
        {
            return Errors.NotFound($"There is no event with id {eventId}.");
        }

        var latest = await db.WeatherSnapshots.AsNoTracking()
            .Where(w => w.EventId == eventId)
            .OrderByDescending(w => w.CapturedAt)
            .Select(w => new WeatherSnapshotDto
            {
                SnapshotId = w.SnapshotId,
                EventId = w.EventId,
                CapturedAt = w.CapturedAt,
                TemperatureC = w.TemperatureC,
                FeelsLikeC = w.FeelsLikeC,
                Conditions = w.Conditions,
                WindSpeedKph = w.WindSpeedKph,
                HumidityPercent = w.HumidityPercent,
                ChanceOfRainPct = w.ChanceOfRainPct,
                Source = w.Source
            })
            .FirstOrDefaultAsync(cancellationToken);

        return latest is null
            ? Errors.Unavailable("No weather reading has been cached for this event yet, and the live weather provider is not available.")
            : latest;
    }
}
