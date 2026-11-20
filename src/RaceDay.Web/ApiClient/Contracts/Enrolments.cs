namespace RaceDay.Web.ApiClient.Contracts;

public sealed class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public TimeOnly StartTime { get; set; }
    public string RaceNumber { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string EnrolmentStatus { get; set; } = string.Empty;
    public decimal EntryFee { get; set; }
    public decimal AmountPaid { get; set; }
    public string? PaymentReference { get; set; }
    public string? TShirtSize { get; set; }
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public bool HasResult { get; set; }

    public bool IsActive => EnrolmentStatus is EnrolmentStatuses.Pending or EnrolmentStatuses.Confirmed;
}

public sealed class EnrolmentRequest
{
    public string? TShirtSize { get; set; }
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
}

public sealed class PaymentRequest
{
    public decimal? AmountPaid { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
}
