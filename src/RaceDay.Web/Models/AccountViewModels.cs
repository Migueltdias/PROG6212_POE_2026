using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Enter your first name.")]
    [StringLength(60)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your surname.")]
    [StringLength(60)]
    [Display(Name = "Surname")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(150)]
    [Display(Name = "Email address")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [RegularExpression(@"^\+?[0-9]{10,15}$", ErrorMessage = "Phone numbers are 10 to 15 digits, optionally starting with +.")]
    [Display(Name = "Mobile number")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Enter your date of birth.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Choose a gender.")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose how you will use RaceDay.")]
    [Display(Name = "I want to")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Use at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Type the password again.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
