namespace RaceDay.Web.ApiClient.Contracts;

public sealed class RaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string RaceNumber { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? ChipTimeSeconds { get; set; }
    public int? OverallPosition { get; set; }
    public int? GenderPosition { get; set; }
    public int? AgeGroupPosition { get; set; }
    public int FinishersInCategory { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }
}

public sealed class ResultRequest
{
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? ChipTimeSeconds { get; set; }
    public int? OverallPosition { get; set; }
    public int? GenderPosition { get; set; }
    public int? AgeGroupPosition { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
}

public sealed class LeaderboardEntry
{
    public int ResultId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int EnrolmentId { get; set; }
    public string RaceNumber { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string ResultStatus { get; set; } = string.Empty;
    public TimeOnly? FinishTime { get; set; }
    public int? ChipTimeSeconds { get; set; }
    public int? OverallPosition { get; set; }
    public int? GenderPosition { get; set; }
    public int? AgeGroupPosition { get; set; }
}

public sealed class BulkResultsRequest
{
    public List<BulkResultRow> Results { get; set; } = [];
}

public sealed class BulkResultRow
{
    public string RaceNumber { get; set; } = string.Empty;
    public TimeOnly? FinishTime { get; set; }
    public int? ChipTimeSeconds { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
}

public sealed class BulkImportResponse
{
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<BulkImportError> Errors { get; set; } = [];
}

public sealed class BulkImportError
{
    public string RaceNumber { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public sealed class EventResults
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string EventStatus { get; set; } = string.Empty;
    public List<CategoryResults> Categories { get; set; } = [];
}

public sealed class CategoryResults
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public List<LeaderboardEntry> Results { get; set; } = [];
}
