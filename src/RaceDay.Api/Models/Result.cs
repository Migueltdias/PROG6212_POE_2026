namespace RaceDay.Api.Models;

public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int CapturedByUserId { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly? FinishTime { get; set; }
    public int? ChipTimeSeconds { get; set; }
    public int? OverallPosition { get; set; }
    public int? GenderPosition { get; set; }
    public int? AgeGroupPosition { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public DateTime CapturedAt { get; set; }

    public Enrolment Enrolment { get; set; } = null!;
    public User CapturedBy { get; set; } = null!;
}
