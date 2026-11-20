using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Domain;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>One point on a category's route.</summary>
public class WaypointDto
{
    /// <example>5</example>
    public int WaypointId { get; set; }

    /// <example>1</example>
    public int CategoryId { get; set; }

    /// <example>5</example>
    public int SequenceNumber { get; set; }

    /// <example>Drummond Halfway</example>
    public string WaypointName { get; set; } = string.Empty;

    /// <summary>Start, WaterPoint, Medical, Timing or Finish.</summary>
    /// <example>Timing</example>
    public string WaypointType { get; set; } = string.Empty;

    /// <example>-29.7553</example>
    public decimal Latitude { get; set; }

    /// <example>30.6889</example>
    public decimal Longitude { get; set; }

    /// <example>43.85</example>
    public decimal DistanceFromStartKm { get; set; }
}

/// <summary>A point on a route.</summary>
public class WaypointRequest
{
    /// <summary>Position on the route, from 1. Unique within the category.</summary>
    /// <example>10</example>
    [Required, Range(1, 10_000)]
    public int? SequenceNumber { get; set; }

    /// <example>Lion Park Water Table</example>
    [Required, StringLength(80)]
    public string WaypointName { get; set; } = string.Empty;

    /// <summary>Start, WaterPoint, Medical, Timing or Finish.</summary>
    /// <example>WaterPoint</example>
    [Required, OneOf(typeof(WaypointTypes))]
    public string WaypointType { get; set; } = string.Empty;

    /// <example>-26.2410</example>
    [Required, Range(-90, 90)]
    public decimal? Latitude { get; set; }

    /// <example>27.9050</example>
    [Required, Range(-180, 180)]
    public decimal? Longitude { get; set; }

    /// <summary>Kilometres from the start. Cannot exceed the category's distance.</summary>
    /// <example>12.5</example>
    [Required, Range(0, 9999.99)]
    public decimal? DistanceFromStartKm { get; set; }
}

public static class WaypointMappings
{
    public static readonly Expression<Func<RouteWaypoint, WaypointDto>> ToDto = w => new WaypointDto
    {
        WaypointId = w.WaypointId,
        CategoryId = w.CategoryId,
        SequenceNumber = w.SequenceNumber,
        WaypointName = w.WaypointName,
        WaypointType = w.WaypointType,
        Latitude = w.Latitude,
        Longitude = w.Longitude,
        DistanceFromStartKm = w.DistanceFromStartKm
    };

    private static readonly Func<RouteWaypoint, WaypointDto> ToDtoCompiled = ToDto.Compile();

    public static WaypointDto ToWaypointDto(this RouteWaypoint waypoint) => ToDtoCompiled(waypoint);

    public static void ApplyTo(this WaypointRequest request, RouteWaypoint waypoint)
    {
        waypoint.SequenceNumber = request.SequenceNumber!.Value;
        waypoint.WaypointName = request.WaypointName.Trim();
        waypoint.WaypointType = DomainValue.Canonical(request.WaypointType, WaypointTypes.All)!;
        waypoint.Latitude = request.Latitude!.Value;
        waypoint.Longitude = request.Longitude!.Value;
        waypoint.DistanceFromStartKm = request.DistanceFromStartKm!.Value;
    }
}
