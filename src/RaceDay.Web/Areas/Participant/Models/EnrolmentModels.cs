using System.ComponentModel.DataAnnotations;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Areas.Participant.Models;

public class EntryDetailsInput
{
    [Display(Name = "T-shirt size")]
    public string? TShirtSize { get; set; }

    [Required(ErrorMessage = "Enter an emergency contact.")]
    [StringLength(100)]
    [Display(Name = "Emergency contact name")]
    public string EmergencyContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the emergency contact's phone number.")]
    [StringLength(20)]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Phone numbers are 10 to 15 digits, optionally starting with +.")]
    [Display(Name = "Emergency contact phone")]
    public string EmergencyContactPhone { get; set; } = string.Empty;

    public EnrolmentRequest ToRequest() => new()
    {
        TShirtSize = string.IsNullOrWhiteSpace(TShirtSize) ? null : TShirtSize,
        EmergencyContactName = EmergencyContactName.Trim(),
        EmergencyContactPhone = EmergencyContactPhone.Trim()
    };
}

public sealed class EnterViewModel : EntryDetailsInput
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Choose the category you want to enter.")]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    public EventDetail? Event { get; set; }
}

public sealed class EditEntryViewModel : EntryDetailsInput
{
    public Enrolment? Enrolment { get; set; }
}

public sealed class MyEventsViewModel
{
    public string Tab { get; init; } = "upcoming";
    public PagedResult<Enrolment> Entries { get; init; } = new();
    public DateOnly Today { get; init; }

    public bool CanChange(Enrolment entry) => entry.IsActive && entry.EventDate > Today;
}
