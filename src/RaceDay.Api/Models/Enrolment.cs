namespace RaceDay.Api.Models;

public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public string RaceNumber { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string EnrolmentStatus { get; set; } = Domain.EnrolmentStatuses.Pending;
    public decimal AmountPaid { get; set; }
    public string? PaymentReference { get; set; }
    public string? TShirtSize { get; set; }
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;

    public User Participant { get; set; } = null!;
    public EventCategory Category { get; set; } = null!;
    public Result? Result { get; set; }
}
