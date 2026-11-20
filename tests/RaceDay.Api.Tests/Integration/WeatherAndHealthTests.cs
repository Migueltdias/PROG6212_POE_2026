using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class WeatherAndHealthTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task Weather_returns_the_most_recent_cached_reading()
    {
        var weather = await factory.CreateAnonymousClient().GetFromJsonAsync<WeatherSnapshotDto>("/api/events/2/weather");

        Assert.Equal(4, weather!.SnapshotId);
        Assert.Equal(21.3m, weather.TemperatureC);
        Assert.Equal("Sunny", weather.Conditions);
    }

    [Fact]
    public async Task Weather_for_an_event_with_nothing_cached_returns_503()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var ev = await TestData.CreateEventAsync(thandeka, publish: true, TestData.NewCategory());

        var response = await factory.CreateAnonymousClient().GetAsync($"/api/events/{ev.EventId}/weather");

        await response.ShouldHaveStatusAsync(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Weather_for_an_unknown_event_returns_404()
    {
        var response = await factory.CreateAnonymousClient().GetAsync("/api/events/9999/weather");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Health_reports_the_API_and_database_status()
    {
        var health = await factory.CreateAnonymousClient().GetFromJsonAsync<HealthResponse>("/api/health");

        Assert.Equal("Healthy", health!.Status);
        Assert.Equal("Connected", health.Database);
        Assert.Equal("2.0.0", health.Version);
    }
}
