using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Domain;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>A captured result with the event, category and participant it belongs to.</summary>
public class ResultDto
{
    /// <example>1</example>
    public int ResultId { get; set; }

    /// <example>1</example>
    public int EnrolmentId { get; set; }

    /// <example>CM1042</example>
    public string RaceNumber { get; set; } = string.Empty;

    /// <example>3</example>
    public int ParticipantId { get; set; }

    /// <example>Sipho Ndlovu</example>
    public string ParticipantName { get; set; } = string.Empty;

    /// <example>M</example>
    public string Gender { get; set; } = string.Empty;

    /// <example>1</example>
    public int EventId { get; set; }

    /// <example>Comrades Marathon 2026</example>
    public string EventName { get; set; } = string.Empty;

    /// <example>2026-06-14</example>
    public DateOnly EventDate { get; set; }

    /// <example>1</example>
    public int CategoryId { get; set; }

    /// <example>Up Run 87.7km Ultra Marathon</example>
    public string CategoryName { get; set; } = string.Empty;

    /// <example>Run</example>
    public string Discipline { get; set; } = string.Empty;

    /// <example>87.7</example>
    public decimal DistanceKm { get; set; }

    /// <example>05:30:00</example>
    public TimeOnly StartTime { get; set; }

    /// <example>11:12:47</example>
    public TimeOnly? FinishTime { get; set; }

    /// <example>20567</example>
    public int? ChipTimeSeconds { get; set; }

    /// <summary>The chip time as h:mm:ss, for display.</summary>
    /// <example>05:42:47</example>
    public string? ChipTime => ResultFormatting.FormatSeconds(ChipTimeSeconds);

    /// <example>1</example>
    public int? OverallPosition { get; set; }

    /// <example>1</example>
    public int? GenderPosition { get; set; }

    /// <example>1</example>
    public int? AgeGroupPosition { get; set; }

    /// <summary>How many people finished this category, so a position reads as e.g. "47th of 312".</summary>
    /// <example>3</example>
    public int FinishersInCategory { get; set; }

    /// <summary>Finished, DNF (did not finish), DNS (did not start) or DQ (disqualified).</summary>
    /// <example>Finished</example>
    public string ResultStatus { get; set; } = string.Empty;

    public DateTime CapturedAt { get; set; }
    public int CapturedByUserId { get; set; }
}

/// <summary>
/// The outcome of one entry. A finisher needs a finish time and an overall position; any other
/// status must leave the time and positions empty, mirroring CK_Results_Consistency.
/// </summary>
public class ResultRequest : IValidatableObject
{
    /// <summary>Gun start time (HH:mm:ss). Defaults to the category's start time.</summary>
    /// <example>06:30:00</example>
    public TimeOnly? StartTime { get; set; }

    /// <summary>Required for Finished, must be empty otherwise.</summary>
    /// <example>08:02:45</example>
    public TimeOnly? FinishTime { get; set; }

    /// <summary>Net time in seconds. Worked out from the start and finish times when left out.</summary>
    /// <example>5565</example>
    [Range(1, int.MaxValue)]
    public int? ChipTimeSeconds { get; set; }

    /// <summary>Required for Finished.</summary>
    /// <example>3</example>
    [Range(1, int.MaxValue)]
    public int? OverallPosition { get; set; }

    /// <example>2</example>
    [Range(1, int.MaxValue)]
    public int? GenderPosition { get; set; }

    /// <example>1</example>
    [Range(1, int.MaxValue)]
    public int? AgeGroupPosition { get; set; }

    /// <summary>Finished, DNF, DNS or DQ.</summary>
    /// <example>Finished</example>
    [Required, OneOf(typeof(ResultStatuses))]
    public string ResultStatus { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var status = DomainValue.Canonical(ResultStatus, ResultStatuses.All);
        if (status is null)
        {
            yield break;
        }

        if (status == ResultStatuses.Finished)
        {
            if (FinishTime is null)
            {
                yield return new ValidationResult("A finisher needs a finish time.", [JsonName.Of(nameof(FinishTime))]);
            }

            if (OverallPosition is null)
            {
                yield return new ValidationResult("A finisher needs an overall position.", [JsonName.Of(nameof(OverallPosition))]);
            }

            yield break;
        }

        if (FinishTime is not null || ChipTimeSeconds is not null)
        {
            yield return new ValidationResult($"A finish time was sent for a {status} result; only finishers have one.", [JsonName.Of(nameof(FinishTime))]);
        }

        if (OverallPosition is not null || GenderPosition is not null || AgeGroupPosition is not null)
        {
            yield return new ValidationResult($"A {status} result cannot have a finishing position.", [JsonName.Of(nameof(OverallPosition))]);
        }
    }
}

public static class ResultFormatting
{
    public static string? FormatSeconds(int? seconds) =>
        seconds is { } s ? $"{s / 3600:D2}:{s % 3600 / 60:D2}:{s % 60:D2}" : null;
}

public static class ResultMappings
{
    public static readonly Expression<Func<Result, ResultDto>> ToDto = r => new ResultDto
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        RaceNumber = r.Enrolment.RaceNumber,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
        Gender = r.Enrolment.Participant.Gender,
        EventId = r.Enrolment.Category.EventId,
        EventName = r.Enrolment.Category.Event.EventName,
        EventDate = r.Enrolment.Category.Event.EventDate,
        CategoryId = r.Enrolment.CategoryId,
        CategoryName = r.Enrolment.Category.CategoryName,
        Discipline = r.Enrolment.Category.Discipline,
        DistanceKm = r.Enrolment.Category.DistanceKm,
        StartTime = r.StartTime,
        FinishTime = r.FinishTime,
        ChipTimeSeconds = r.ChipTimeSeconds,
        OverallPosition = r.OverallPosition,
        GenderPosition = r.GenderPosition,
        AgeGroupPosition = r.AgeGroupPosition,
        FinishersInCategory = r.Enrolment.Category.Enrolments
            .Count(en => en.Result != null && en.Result.ResultStatus == ResultStatuses.Finished),
        ResultStatus = r.ResultStatus,
        CapturedAt = r.CapturedAt,
        CapturedByUserId = r.CapturedByUserId
    };
}

/// <summary>One row of a category leaderboard. The columns match the vw_CategoryLeaderboard view, plus the result id.</summary>
public class LeaderboardEntryDto
{
    /// <summary>The result's id, for correcting or removing it.</summary>
    /// <example>1</example>
    public int ResultId { get; set; }

    public int EventId { get; set; }

    /// <example>Comrades Marathon 2026</example>
    public string EventName { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    /// <example>Up Run 87.7km Ultra Marathon</example>
    public string CategoryName { get; set; } = string.Empty;

    /// <example>87.7</example>
    public decimal DistanceKm { get; set; }

    public int EnrolmentId { get; set; }

    /// <example>CM1042</example>
    public string RaceNumber { get; set; } = string.Empty;

    public int ParticipantId { get; set; }

    /// <example>Sipho Ndlovu</example>
    public string ParticipantName { get; set; } = string.Empty;

    /// <example>M</example>
    public string Gender { get; set; } = string.Empty;

    /// <example>Finished</example>
    public string ResultStatus { get; set; } = string.Empty;

    /// <example>11:12:47</example>
    public TimeOnly? FinishTime { get; set; }

    /// <example>20567</example>
    public int? ChipTimeSeconds { get; set; }

    /// <example>05:42:47</example>
    public string? ChipTime => ResultFormatting.FormatSeconds(ChipTimeSeconds);

    /// <example>1</example>
    public int? OverallPosition { get; set; }

    /// <example>1</example>
    public int? GenderPosition { get; set; }

    /// <example>1</example>
    public int? AgeGroupPosition { get; set; }
}

/// <summary>Results imported from a timing system for one category.</summary>
public class BulkResultsRequest
{
    /// <summary>One row per race number, 1 to 5000 rows.</summary>
    [Required, MinLength(1), MaxLength(5000)]
    public List<BulkResultRow> Results { get; set; } = [];
}

/// <summary>One line of a timing file.</summary>
public class BulkResultRow
{
    /// <example>CC0073</example>
    [Required, StringLength(10)]
    public string RaceNumber { get; set; } = string.Empty;

    /// <summary>For finishers. Worked out from the chip time when left out.</summary>
    /// <example>08:01:30</example>
    public TimeOnly? FinishTime { get; set; }

    /// <summary>For finishers. Worked out from the finish time when left out.</summary>
    /// <example>3690</example>
    [Range(1, int.MaxValue)]
    public int? ChipTimeSeconds { get; set; }

    /// <summary>Finished, DNF, DNS or DQ.</summary>
    /// <example>Finished</example>
    [Required, OneOf(typeof(ResultStatuses))]
    public string ResultStatus { get; set; } = string.Empty;
}

/// <summary>What happened to each row of a bulk import.</summary>
public class BulkImportResponse
{
    /// <summary>Rows saved as new results.</summary>
    public int Imported { get; set; }

    /// <summary>Rows ignored because that entry already had a result.</summary>
    public int Skipped { get; set; }

    /// <summary>Rows that could not be imported, and why.</summary>
    public List<BulkImportError> Errors { get; set; } = [];
}

public class BulkImportError
{
    /// <example>XX9999</example>
    public string RaceNumber { get; set; } = string.Empty;

    /// <example>No entry in this category has that race number.</example>
    public string Error { get; set; } = string.Empty;
}

/// <summary>Filters for a category leaderboard.</summary>
public class LeaderboardQuery : PagingQuery
{
    /// <summary>Only this gender (M, F or O); rows are then ordered by gender position.</summary>
    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "gender")]
    [OneOf(typeof(Genders))]
    public string? Gender { get; set; }
}

/// <summary>Filters for a Participant's results history.</summary>
public class MyResultsQuery : PagingQuery, IValidatableObject
{
    /// <summary>Only events on or after this date.</summary>
    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "fromDate")]
    public DateOnly? FromDate { get; set; }

    /// <summary>Only events on or before this date.</summary>
    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "toDate")]
    public DateOnly? ToDate { get; set; }

    /// <summary>Only this event type: Run, Walk or Cycle.</summary>
    [Microsoft.AspNetCore.Mvc.FromQuery(Name = "discipline")]
    [OneOf(typeof(Disciplines))]
    public string? Discipline { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromDate is { } from && ToDate is { } to && to < from)
        {
            yield return new ValidationResult("toDate cannot be before fromDate.", [JsonName.Of(nameof(ToDate))]);
        }
    }
}

public static class LeaderboardMappings
{
    public static readonly Expression<Func<Result, LeaderboardEntryDto>> ToEntry = r => new LeaderboardEntryDto
    {
        ResultId = r.ResultId,
        EventId = r.Enrolment.Category.EventId,
        EventName = r.Enrolment.Category.Event.EventName,
        CategoryId = r.Enrolment.CategoryId,
        CategoryName = r.Enrolment.Category.CategoryName,
        DistanceKm = r.Enrolment.Category.DistanceKm,
        EnrolmentId = r.EnrolmentId,
        RaceNumber = r.Enrolment.RaceNumber,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
        Gender = r.Enrolment.Participant.Gender,
        ResultStatus = r.ResultStatus,
        FinishTime = r.FinishTime,
        ChipTimeSeconds = r.ChipTimeSeconds,
        OverallPosition = r.OverallPosition,
        GenderPosition = r.GenderPosition,
        AgeGroupPosition = r.AgeGroupPosition
    };

    public static IQueryable<Result> InLeaderboardOrder(this IQueryable<Result> results, bool byGender) =>
        byGender
            ? results.OrderBy(r => r.GenderPosition == null).ThenBy(r => r.GenderPosition).ThenBy(r => r.ResultStatus).ThenBy(r => r.Enrolment.RaceNumber)
            : results.OrderBy(r => r.OverallPosition == null).ThenBy(r => r.OverallPosition).ThenBy(r => r.ResultStatus).ThenBy(r => r.Enrolment.RaceNumber);
}

/// <summary>An event's results, one leaderboard per category.</summary>
public class EventResultsDto
{
    /// <example>1</example>
    public int EventId { get; set; }

    /// <example>Comrades Marathon 2026</example>
    public string EventName { get; set; } = string.Empty;

    /// <example>2026-06-14</example>
    public DateOnly EventDate { get; set; }

    /// <example>Completed</example>
    public string EventStatus { get; set; } = string.Empty;

    public List<CategoryResultsDto> Categories { get; set; } = [];
}

/// <summary>The leaderboard for one category of an event.</summary>
public class CategoryResultsDto
{
    public int CategoryId { get; set; }

    /// <example>Up Run 87.7km Ultra Marathon</example>
    public string CategoryName { get; set; } = string.Empty;

    /// <example>Run</example>
    public string Discipline { get; set; } = string.Empty;

    /// <example>87.7</example>
    public decimal DistanceKm { get; set; }

    public List<LeaderboardEntryDto> Results { get; set; } = [];
}
