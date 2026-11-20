using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Domain;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>A distance offered at an event, with how many places are left.</summary>
public class CategoryDto
{
    /// <example>4</example>
    public int CategoryId { get; set; }

    /// <example>2</example>
    public int EventId { get; set; }

    /// <example>21.1km Half Marathon</example>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Run, Walk or Cycle.</summary>
    /// <example>Run</example>
    public string Discipline { get; set; } = string.Empty;

    /// <example>21.1</example>
    public decimal DistanceKm { get; set; }

    /// <summary>Entry fee in Rand.</summary>
    /// <example>300.00</example>
    public decimal EntryFee { get; set; }

    /// <example>12000</example>
    public int MaxParticipants { get; set; }

    /// <example>06:30:00</example>
    public TimeOnly StartTime { get; set; }

    /// <summary>Minimum age on race day.</summary>
    /// <example>16</example>
    public int MinimumAge { get; set; }

    /// <summary>Entries holding a place (Pending or Confirmed).</summary>
    public int EnteredCount { get; set; }

    public int PlacesLeft { get; set; }
}

/// <summary>
/// A distance or age category for an event, such as "10km", "21.1km Half Marathon" or
/// "Under 20 10km". The discipline is the event type: Run, Walk or Cycle.
/// </summary>
public class CategoryRequest
{
    /// <example>21.1km Half Marathon</example>
    [Required, StringLength(80)]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Run, Walk or Cycle.</summary>
    /// <example>Run</example>
    [Required, OneOf(typeof(Disciplines))]
    public string Discipline { get; set; } = string.Empty;

    /// <example>21.1</example>
    [Required, Range(0.01, 9999.99)]
    public decimal? DistanceKm { get; set; }

    /// <summary>Entry fee in Rand. Zero for a free category, which confirms entries immediately.</summary>
    /// <example>300.00</example>
    [Required, Range(0, 99999999.99)]
    public decimal? EntryFee { get; set; }

    /// <example>5000</example>
    [Required, Range(1, 1_000_000)]
    public int? MaxParticipants { get; set; }

    /// <summary>Start time for this distance (HH:mm:ss).</summary>
    /// <example>06:30:00</example>
    [Required]
    public TimeOnly? StartTime { get; set; }

    /// <summary>Minimum age on race day, 0 to 100. Defaults to 12.</summary>
    /// <example>16</example>
    [Range(0, 100)]
    public int? MinimumAge { get; set; }
}

public static class CategoryMappings
{
    public static readonly Expression<Func<EventCategory, CategoryDto>> ToDto = c => new CategoryDto
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        Discipline = c.Discipline,
        DistanceKm = c.DistanceKm,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants,
        StartTime = c.StartTime,
        MinimumAge = c.MinimumAge,
        EnteredCount = c.Enrolments.Count(en => en.EnrolmentStatus == EnrolmentStatuses.Pending
                                                || en.EnrolmentStatus == EnrolmentStatuses.Confirmed),
        PlacesLeft = c.MaxParticipants - c.Enrolments.Count(en => en.EnrolmentStatus == EnrolmentStatuses.Pending
                                                                  || en.EnrolmentStatus == EnrolmentStatuses.Confirmed)
    };

    public static EventCategory ToEntity(this CategoryRequest request, int eventId) => new()
    {
        EventId = eventId,
        CategoryName = request.CategoryName.Trim(),
        Discipline = DomainValue.Canonical(request.Discipline, Disciplines.All)!,
        DistanceKm = request.DistanceKm!.Value,
        EntryFee = request.EntryFee!.Value,
        MaxParticipants = request.MaxParticipants!.Value,
        StartTime = request.StartTime!.Value,
        MinimumAge = request.MinimumAge ?? 12
    };
}
