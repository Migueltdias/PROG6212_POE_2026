namespace RaceDay.Api.Models;

public class EventCategory
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
    public TimeOnly StartTime { get; set; }
    public int MinimumAge { get; set; } = 12;

    public Event Event { get; set; } = null!;
    public ICollection<RouteWaypoint> Waypoints { get; set; } = new List<RouteWaypoint>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
