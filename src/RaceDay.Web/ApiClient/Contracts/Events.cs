namespace RaceDay.Web.ApiClient.Contracts;

public class EventSummary
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public string EventStatus { get; set; } = string.Empty;
    public DateOnly RegistrationOpens { get; set; }
    public DateOnly RegistrationCloses { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public List<string> Disciplines { get; set; } = [];
    public decimal? ShortestDistanceKm { get; set; }
    public decimal? LongestDistanceKm { get; set; }
    public int CategoryCount { get; set; }
    public int EnteredCount { get; set; }
    public int ConfirmedCount { get; set; }
    public string? BannerImageUrl { get; set; }

    public int PendingCount => EnteredCount - ConfirmedCount;
}

public sealed class EventDetail : EventSummary
{
    public string? Description { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<Category> Categories { get; set; } = [];
}

public class EventRequest
{
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? EventDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public DateOnly? RegistrationOpens { get; set; }
    public DateOnly? RegistrationCloses { get; set; }
}

public sealed class CreateEventRequest : EventRequest
{
    public List<CategoryRequest>? Categories { get; set; }
}

public sealed class EventQuery
{
    public string? Province { get; set; }
    public string? Discipline { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public sealed class WeatherSnapshot
{
    public DateTime CapturedAt { get; set; }
    public decimal TemperatureC { get; set; }
    public decimal? FeelsLikeC { get; set; }
    public string Conditions { get; set; } = string.Empty;
    public decimal? WindSpeedKph { get; set; }
    public byte? HumidityPercent { get; set; }
    public byte? ChanceOfRainPct { get; set; }
    public string Source { get; set; } = string.Empty;
}
