namespace RaceDay.Api.Dtos;

/// <summary>A cached weather reading for an event's venue.</summary>
public class WeatherSnapshotDto
{
    public int SnapshotId { get; set; }
    public int EventId { get; set; }

    /// <summary>When the reading was taken (UTC).</summary>
    public DateTime CapturedAt { get; set; }

    /// <example>21.3</example>
    public decimal TemperatureC { get; set; }

    /// <example>21.0</example>
    public decimal? FeelsLikeC { get; set; }

    /// <example>Sunny</example>
    public string Conditions { get; set; } = string.Empty;

    /// <example>9.4</example>
    public decimal? WindSpeedKph { get; set; }

    /// <example>41</example>
    public byte? HumidityPercent { get; set; }

    /// <example>10</example>
    public byte? ChanceOfRainPct { get; set; }

    /// <example>OpenWeatherMap</example>
    public string Source { get; set; } = string.Empty;
}
