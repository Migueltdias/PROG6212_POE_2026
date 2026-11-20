using System.ComponentModel.DataAnnotations;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Areas.Organiser.Models;

public sealed record CategoryResultsSummary(Category Category, int ConfirmedEntries, IReadOnlyList<LeaderboardEntry> Results);

public sealed class ResultsOverviewViewModel
{
    public EventDetail Event { get; init; } = new();
    public IReadOnlyList<CategoryResultsSummary> Categories { get; init; } = [];
    public DateOnly Today { get; init; }

    public bool HasBeenRun => Event.EventDate <= Today;
    public bool CanComplete => Event.EventStatus == EventStatuses.Published && HasBeenRun;
    public int TotalConfirmed => Categories.Sum(c => c.ConfirmedEntries);
    public int TotalCaptured => Categories.Sum(c => c.Results.Count);
}

public sealed class CaptureRow
{
    public int EnrolmentId { get; init; }
    public string RaceNumber { get; init; } = string.Empty;
    public string ParticipantName { get; init; } = string.Empty;
    public LeaderboardEntry? Existing { get; init; }
}

public sealed class CaptureViewModel
{
    public EventDetail Event { get; init; } = new();
    public Category Category { get; init; } = new();
    public IReadOnlyList<CaptureRow> Rows { get; init; } = [];
    public DateOnly Today { get; init; }

    public bool HasBeenRun => Event.EventDate <= Today;
    public bool CanCapture => HasBeenRun && Event.EventStatus is EventStatuses.Published or EventStatuses.Completed;
    public int Outstanding => Rows.Count(r => r.Existing is null);
}

public sealed class CaptureInput
{
    public string RaceNumber { get; set; } = string.Empty;
    public string? ResultStatus { get; set; }
    public TimeOnly? FinishTime { get; set; }
}

public sealed class ResultEditViewModel
{
    public int ResultId { get; set; }
    public RaceResult? Result { get; set; }

    [Required(ErrorMessage = "Choose the result.")]
    [Display(Name = "Result")]
    public string ResultStatus { get; set; } = ResultStatuses.Finished;

    [Display(Name = "Finish time (clock)")]
    public TimeOnly? FinishTime { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Positions start at 1.")]
    [Display(Name = "Overall position")]
    public int? OverallPosition { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Positions start at 1.")]
    [Display(Name = "Gender position")]
    public int? GenderPosition { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Positions start at 1.")]
    [Display(Name = "Age-group position")]
    public int? AgeGroupPosition { get; set; }

    public static ResultEditViewModel From(RaceResult result) => new()
    {
        ResultId = result.ResultId,
        Result = result,
        ResultStatus = result.ResultStatus,
        FinishTime = result.FinishTime,
        OverallPosition = result.OverallPosition,
        GenderPosition = result.GenderPosition,
        AgeGroupPosition = result.AgeGroupPosition
    };

    public ResultRequest ToRequest()
    {
        var finished = ResultStatus == ResultStatuses.Finished;
        return new ResultRequest
        {
            ResultStatus = ResultStatus,
            FinishTime = finished ? FinishTime : null,
            OverallPosition = finished ? OverallPosition : null,
            GenderPosition = finished ? GenderPosition : null,
            AgeGroupPosition = finished ? AgeGroupPosition : null
        };
    }
}
