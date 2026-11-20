namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public string Province { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string VenueName { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public DateOnly RegistrationOpens { get; set; }
    public DateOnly RegistrationCloses { get; set; }
    public string EventStatus { get; set; } = Domain.EventStatuses.Draft;
    public string? BannerImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Organiser { get; set; } = null!;
    public ICollection<EventCategory> Categories { get; set; } = new List<EventCategory>();
    public ICollection<WeatherSnapshot> WeatherSnapshots { get; set; } = new List<WeatherSnapshot>();
}
