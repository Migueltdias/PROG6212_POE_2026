namespace RaceDay.Api.Dtos;

/// <summary>API and database status.</summary>
public class HealthResponse
{
    /// <summary>Healthy when the database answered, otherwise Unhealthy.</summary>
    /// <example>Healthy</example>
    public string Status { get; set; } = string.Empty;

    /// <summary>Connected or Unreachable.</summary>
    /// <example>Connected</example>
    public string Database { get; set; } = string.Empty;

    /// <summary>The API build version.</summary>
    /// <example>2.0.0</example>
    public string Version { get; set; } = string.Empty;
}
