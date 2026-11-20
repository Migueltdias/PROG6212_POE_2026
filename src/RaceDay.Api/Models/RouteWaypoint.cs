namespace RaceDay.Api.Models;

public class RouteWaypoint
{
    public int WaypointId { get; set; }
    public int CategoryId { get; set; }
    public int SequenceNumber { get; set; }
    public string WaypointName { get; set; } = string.Empty;
    public string WaypointType { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public decimal DistanceFromStartKm { get; set; }

    public EventCategory Category { get; set; } = null!;
}
