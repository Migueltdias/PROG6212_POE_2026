using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Domain;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>An event as shown in a list.</summary>
public class EventSummaryDto
{
    /// <example>2</example>
    public int EventId { get; set; }

    /// <example>Soweto Marathon 2026</example>
    public string EventName { get; set; } = string.Empty;

    /// <example>2026-11-01</example>
    public DateOnly EventDate { get; set; }

    /// <example>06:00:00</example>
    public TimeOnly StartTime { get; set; }

    /// <example>Gauteng</example>
    public string Province { get; set; } = string.Empty;

    /// <example>Soweto</example>
    public string City { get; set; } = string.Empty;

    /// <example>FNB Stadium</example>
    public string VenueName { get; set; } = string.Empty;

    /// <summary>Draft, Published, Cancelled or Completed.</summary>
    /// <example>Published</example>
    public string EventStatus { get; set; } = string.Empty;

    public DateOnly RegistrationOpens { get; set; }
    public DateOnly RegistrationCloses { get; set; }
    public int OrganiserId { get; set; }

    /// <example>Pieter van Wyk</example>
    public string OrganiserName { get; set; } = string.Empty;

    /// <summary>The event types on offer across its categories: Run, Walk and/or Cycle.</summary>
    [JsonInclude]
    public IReadOnlyList<string> Disciplines
    {
        get => CategoryDisciplines.Distinct().Order().ToList();
        private set => CategoryDisciplines = value.ToList();
    }

    /// <summary>The shortest distance offered, in km.</summary>
    /// <example>10.0</example>
    public decimal? ShortestDistanceKm { get; set; }

    /// <summary>The longest distance offered, in km.</summary>
    /// <example>42.2</example>
    public decimal? LongestDistanceKm { get; set; }

    public int CategoryCount { get; set; }

    /// <summary>Entries holding a place across every category (Pending or Confirmed).</summary>
    /// <example>3</example>
    public int EnteredCount { get; set; }

    /// <summary>Entries that have been paid for and confirmed.</summary>
    /// <example>2</example>
    public int ConfirmedCount { get; set; }

    /// <summary>A read-only link to the banner image in Azure Blob Storage, or null if there is none.</summary>
    public string? BannerImageUrl { get; set; }

    [JsonIgnore]
    public List<string> CategoryDisciplines { get; set; } = [];
}

/// <summary>An event with its full details and every category on offer.</summary>
public class EventDetailDto : EventSummaryDto
{
    [JsonPropertyOrder(1)]
    public string? Description { get; set; }

    /// <example>-26.2348</example>
    [JsonPropertyOrder(1)]
    public decimal Latitude { get; set; }

    /// <example>27.9821</example>
    [JsonPropertyOrder(1)]
    public decimal Longitude { get; set; }

    [JsonPropertyOrder(1)]
    public DateTime CreatedAt { get; set; }

    /// <summary>The distances on offer, longest first. Participants enter one of these.</summary>
    [JsonPropertyOrder(2)]
    public List<CategoryDto> Categories { get; set; } = [];
}

/// <summary>The details of an event. Distances and event types are set on its categories.</summary>
public class EventRequest : IValidatableObject
{
    /// <example>Durban Beachfront 10km 2027</example>
    [Required, StringLength(120)]
    public string EventName { get; set; } = string.Empty;

    /// <example>A flat out-and-back race along the Golden Mile promenade.</example>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>yyyy-MM-dd. Cannot be in the past, and must be on or after registrationCloses.</summary>
    /// <example>2027-05-09</example>
    [Required]
    public DateOnly? EventDate { get; set; }

    /// <example>06:30:00</example>
    [Required]
    public TimeOnly? StartTime { get; set; }

    /// <summary>One of the nine South African provinces.</summary>
    /// <example>KwaZulu-Natal</example>
    [Required, OneOf(typeof(Provinces))]
    public string Province { get; set; } = string.Empty;

    /// <example>Durban</example>
    [Required, StringLength(80)]
    public string City { get; set; } = string.Empty;

    /// <example>Suncoast Promenade</example>
    [Required, StringLength(120)]
    public string VenueName { get; set; } = string.Empty;

    /// <example>-29.839500</example>
    [Required, Range(-90, 90)]
    public decimal? Latitude { get; set; }

    /// <example>31.035200</example>
    [Required, Range(-180, 180)]
    public decimal? Longitude { get; set; }

    /// <example>2026-11-01</example>
    [Required]
    public DateOnly? RegistrationOpens { get; set; }

    /// <example>2027-04-30</example>
    [Required]
    public DateOnly? RegistrationCloses { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RegistrationOpens is { } opens && RegistrationCloses is { } closes && closes < opens)
        {
            yield return new ValidationResult("Registration cannot close before it opens.", [JsonName.Of(nameof(RegistrationCloses))]);
        }

        if (RegistrationCloses is { } close && EventDate is { } date && date < close)
        {
            yield return new ValidationResult("The event cannot take place before registration closes.", [JsonName.Of(nameof(EventDate))]);
        }
    }
}

/// <summary>A new event. Categories are optional here and can also be added later.</summary>
public class CreateEventRequest : EventRequest
{
    /// <summary>
    /// Optional: the distances to create with the event, so it captures its distance and type
    /// (Run, Walk or Cycle) in one call. Names must be unique within the event.
    /// </summary>
    public List<CategoryRequest>? Categories { get; set; }
}

/// <summary>A new status for an event.</summary>
public class EventStatusRequest
{
    /// <summary>Published, Cancelled or Completed (or Draft, which is never a valid target).</summary>
    /// <example>Published</example>
    [Required, OneOf(typeof(EventStatuses))]
    public string Status { get; set; } = string.Empty;
}

/// <summary>Filters for browsing public events.</summary>
public class EventQuery : PagingQuery, IValidatableObject
{
    /// <summary>Only events in this province.</summary>
    /// <example>Gauteng</example>
    [FromQuery(Name = "province")]
    [OneOf(typeof(Provinces))]
    public string? Province { get; set; }

    /// <summary>Only events offering this event type: Run, Walk or Cycle.</summary>
    /// <example>Run</example>
    [FromQuery(Name = "discipline")]
    [OneOf(typeof(Disciplines))]
    public string? Discipline { get; set; }

    /// <summary>Only events on or after this date (yyyy-MM-dd).</summary>
    [FromQuery(Name = "fromDate")]
    public DateOnly? FromDate { get; set; }

    /// <summary>Only events on or before this date (yyyy-MM-dd).</summary>
    [FromQuery(Name = "toDate")]
    public DateOnly? ToDate { get; set; }

    /// <summary>Text to find in the event name, description, city or venue.</summary>
    /// <example>marathon</example>
    [FromQuery(Name = "search")]
    [StringLength(100)]
    public string? Search { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromDate is { } from && ToDate is { } to && to < from)
        {
            yield return new ValidationResult("toDate cannot be before fromDate.", [JsonName.Of(nameof(ToDate))]);
        }
    }
}

/// <summary>Filters for the Organiser's own events.</summary>
public class MyEventsQuery : PagingQuery
{
    /// <summary>Only events with this status: Draft, Published, Cancelled or Completed.</summary>
    [FromQuery(Name = "status")]
    [OneOf(typeof(EventStatuses))]
    public string? Status { get; set; }
}

public static class EventMappings
{
    public static readonly Expression<Func<Event, EventSummaryDto>> ToSummary = e => new EventSummaryDto
    {
        EventId = e.EventId,
        EventName = e.EventName,
        EventDate = e.EventDate,
        StartTime = e.StartTime,
        Province = e.Province,
        City = e.City,
        VenueName = e.VenueName,
        EventStatus = e.EventStatus,
        RegistrationOpens = e.RegistrationOpens,
        RegistrationCloses = e.RegistrationCloses,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FirstName + " " + e.Organiser.LastName,
        CategoryDisciplines = e.Categories.Select(c => c.Discipline).ToList(),
        ShortestDistanceKm = e.Categories.Min(c => (decimal?)c.DistanceKm),
        LongestDistanceKm = e.Categories.Max(c => (decimal?)c.DistanceKm),
        CategoryCount = e.Categories.Count,
        EnteredCount = e.Categories.SelectMany(c => c.Enrolments)
            .Count(en => en.EnrolmentStatus == EnrolmentStatuses.Pending || en.EnrolmentStatus == EnrolmentStatuses.Confirmed),
        ConfirmedCount = e.Categories.SelectMany(c => c.Enrolments)
            .Count(en => en.EnrolmentStatus == EnrolmentStatuses.Confirmed),
        BannerImageUrl = e.BannerImageUrl
    };

    public static readonly Expression<Func<Event, EventDetailDto>> ToDetail = e => new EventDetailDto
    {
        EventId = e.EventId,
        EventName = e.EventName,
        EventDate = e.EventDate,
        StartTime = e.StartTime,
        Province = e.Province,
        City = e.City,
        VenueName = e.VenueName,
        EventStatus = e.EventStatus,
        RegistrationOpens = e.RegistrationOpens,
        RegistrationCloses = e.RegistrationCloses,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FirstName + " " + e.Organiser.LastName,
        CategoryDisciplines = e.Categories.Select(c => c.Discipline).ToList(),
        ShortestDistanceKm = e.Categories.Min(c => (decimal?)c.DistanceKm),
        LongestDistanceKm = e.Categories.Max(c => (decimal?)c.DistanceKm),
        CategoryCount = e.Categories.Count,
        EnteredCount = e.Categories.SelectMany(c => c.Enrolments)
            .Count(en => en.EnrolmentStatus == EnrolmentStatuses.Pending || en.EnrolmentStatus == EnrolmentStatuses.Confirmed),
        ConfirmedCount = e.Categories.SelectMany(c => c.Enrolments)
            .Count(en => en.EnrolmentStatus == EnrolmentStatuses.Confirmed),
        BannerImageUrl = e.BannerImageUrl,
        Description = e.Description,
        Latitude = e.Latitude,
        Longitude = e.Longitude,
        CreatedAt = e.CreatedAt,
        Categories = e.Categories.AsQueryable()
            .OrderByDescending(c => c.DistanceKm).ThenBy(c => c.CategoryName)
            .Select(CategoryMappings.ToDto)
            .ToList()
    };

    public static IQueryable<Event> VisibleTo(this IQueryable<Event> events, int? callerId) =>
        events.Where(e => e.EventStatus != EventStatuses.Draft || e.OrganiserId == callerId);
}
