using System.ComponentModel.DataAnnotations;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Models;

public sealed class ProfileDetailsInput
{
    [Required(ErrorMessage = "Enter your first name.")]
    [StringLength(60)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your surname.")]
    [StringLength(60)]
    [Display(Name = "Surname")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(20)]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Phone numbers are 10 to 15 digits, optionally starting with +.")]
    [Display(Name = "Mobile number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Enter your date of birth.")]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Choose a gender.")]
    public string Gender { get; set; } = string.Empty;

    public static ProfileDetailsInput From(UserProfile profile) => new()
    {
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        PhoneNumber = profile.PhoneNumber,
        DateOfBirth = profile.DateOfBirth,
        Gender = profile.Gender
    };

    public UpdateProfileRequest ToRequest() => new()
    {
        FirstName = FirstName.Trim(),
        LastName = LastName.Trim(),
        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
        DateOfBirth = DateOfBirth,
        Gender = Gender
    };
}

public sealed class PasswordInput
{
    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a new password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type the new password again.")]
    [Compare(nameof(NewPassword), ErrorMessage = "The new passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public sealed class ProfilePageViewModel
{
    public const long MaxPictureBytes = 5 * 1024 * 1024;

    public UserProfile Profile { get; init; } = new();
    public ProfileDetailsInput Details { get; init; } = new();
    public PasswordInput Password { get; init; } = new();

    public string Initials => string.Concat(new[] { Profile.FirstName, Profile.LastName }
        .Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => char.ToUpperInvariant(n[0])));
}
