using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Domain;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>
/// An entry: the link between a Participant, the category they selected and its event.
/// </summary>
public class EnrolmentDto
{
    /// <example>14</example>
    public int EnrolmentId { get; set; }

    /// <example>5</example>
    public int ParticipantId { get; set; }

    /// <example>Ayesha Patel</example>
    public string ParticipantName { get; set; } = string.Empty;

    /// <example>2</example>
    public int EventId { get; set; }

    /// <example>Soweto Marathon 2026</example>
    public string EventName { get; set; } = string.Empty;

    /// <example>2026-11-01</example>
    public DateOnly EventDate { get; set; }

    /// <example>4</example>
    public int CategoryId { get; set; }

    /// <example>21.1km Half Marathon</example>
    public string CategoryName { get; set; } = string.Empty;

    /// <example>Run</example>
    public string Discipline { get; set; } = string.Empty;

    /// <example>21.1</example>
    public decimal DistanceKm { get; set; }

    /// <example>06:30:00</example>
    public TimeOnly StartTime { get; set; }

    /// <summary>Allocated by the server when the entry is made.</summary>
    /// <example>SM1121</example>
    public string RaceNumber { get; set; } = string.Empty;

    public DateTime EnrolmentDate { get; set; }

    /// <summary>Pending (awaiting payment), Confirmed, Cancelled or Withdrawn.</summary>
    /// <example>Pending</example>
    public string EnrolmentStatus { get; set; } = string.Empty;

    /// <example>300.00</example>
    public decimal EntryFee { get; set; }

    /// <example>0.00</example>
    public decimal AmountPaid { get; set; }

    public string? PaymentReference { get; set; }

    /// <example>M</example>
    public string? TShirtSize { get; set; }

    /// <example>Riaz Patel</example>
    public string EmergencyContactName { get; set; } = string.Empty;

    /// <example>0836549871</example>
    public string EmergencyContactPhone { get; set; } = string.Empty;

    /// <summary>True once the Organiser has captured a result for this entry.</summary>
    public bool HasResult { get; set; }
}

/// <summary>The details a Participant gives when entering (or updating an entry).</summary>
public class EnrolmentRequest
{
    /// <summary>XS, S, M, L, XL or XXL. Optional.</summary>
    /// <example>M</example>
    [OneOf(typeof(TShirtSizes))]
    public string? TShirtSize { get; set; }

    /// <example>Riaz Patel</example>
    [Required, StringLength(100)]
    public string EmergencyContactName { get; set; } = string.Empty;

    /// <example>0836549871</example>
    [Required, StringLength(20)]
    [RegularExpression(ContactRules.PhonePattern, ErrorMessage = ContactRules.PhoneMessage)]
    public string EmergencyContactPhone { get; set; } = string.Empty;
}

/// <summary>Payment of the entry fee.</summary>
public class PaymentRequest
{
    /// <summary>Must equal the category's entry fee exactly.</summary>
    /// <example>300.00</example>
    [Required, Range(0.01, 99999999.99)]
    public decimal? AmountPaid { get; set; }

    /// <example>PAY-SM-000999</example>
    [Required, StringLength(50)]
    public string PaymentReference { get; set; } = string.Empty;
}

/// <summary>Filters for a Participant's own entries.</summary>
public class MyEnrolmentsQuery : PagingQuery
{
    /// <summary>Only entries with this status.</summary>
    [FromQuery(Name = "status")]
    [OneOf(typeof(EnrolmentStatuses))]
    public string? Status { get; set; }

    /// <summary>true: events today or later (soonest first). false: past events. Omit for all.</summary>
    [FromQuery(Name = "upcoming")]
    public bool? Upcoming { get; set; }
}

/// <summary>Filters for a start list.</summary>
public class StartListQuery : PagingQuery
{
    /// <summary>Only entries with this status.</summary>
    [FromQuery(Name = "status")]
    [OneOf(typeof(EnrolmentStatuses))]
    public string? Status { get; set; }
}

/// <summary>Filters for an event-wide start list.</summary>
public class EventStartListQuery : StartListQuery
{
    /// <summary>Only entries in this category of the event.</summary>
    [FromQuery(Name = "categoryId")]
    public int? CategoryId { get; set; }
}

public static class EnrolmentMappings
{
    public static readonly Expression<Func<Enrolment, EnrolmentDto>> ToDto = e => new EnrolmentDto
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
        EventId = e.Category.EventId,
        EventName = e.Category.Event.EventName,
        EventDate = e.Category.Event.EventDate,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.CategoryName,
        Discipline = e.Category.Discipline,
        DistanceKm = e.Category.DistanceKm,
        StartTime = e.Category.StartTime,
        RaceNumber = e.RaceNumber,
        EnrolmentDate = e.EnrolmentDate,
        EnrolmentStatus = e.EnrolmentStatus,
        EntryFee = e.Category.EntryFee,
        AmountPaid = e.AmountPaid,
        PaymentReference = e.PaymentReference,
        TShirtSize = e.TShirtSize,
        EmergencyContactName = e.EmergencyContactName,
        EmergencyContactPhone = e.EmergencyContactPhone,
        HasResult = e.Result != null
    };

    public static void ApplyTo(this EnrolmentRequest request, Enrolment enrolment)
    {
        enrolment.TShirtSize = DomainValue.Canonical(request.TShirtSize, TShirtSizes.All);
        enrolment.EmergencyContactName = request.EmergencyContactName.Trim();
        enrolment.EmergencyContactPhone = request.EmergencyContactPhone.Trim();
    }
}
