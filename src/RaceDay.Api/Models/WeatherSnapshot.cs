namespace RaceDay.Api.Models;

public class WeatherSnapshot
{
    public int SnapshotId { get; set; }
    public int EventId { get; set; }
    public DateTime CapturedAt { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal? FeelsLikeC { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public decimal? WindSpeedKph { get; set; }
    public byte? HumidityPercent { get; set; }
    public byte? ChanceOfRainPct { get; set; }
    public string Source { get; set; } = "OpenWeatherMap";

    public Event Event { get; set; } = null!;
}
