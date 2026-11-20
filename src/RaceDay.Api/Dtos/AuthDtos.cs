using System.ComponentModel.DataAnnotations;
using RaceDay.Api.Domain;
using RaceDay.Api.Infrastructure;

namespace RaceDay.Api.Dtos;

public static class PasswordRules
{
    public const string Pattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$";
    public const string Message =
        "Password must be 8 to 100 characters and contain an upper-case letter, a lower-case letter and a digit.";
}

public static class ContactRules
{
    public const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string EmailMessage = "Enter a valid email address.";
    public const string PhonePattern = @"^\+?[0-9]{10,15}$";
    public const string PhoneMessage = "Phone numbers must be 10 to 15 digits, optionally starting with +.";
}

/// <summary>Details for a new account.</summary>
public class RegisterRequest
{
    /// <example>Ayanda</example>
    [Required, StringLength(60)]
    public string FirstName { get; set; } = string.Empty;

    /// <example>Zulu</example>
    [Required, StringLength(60)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>Used to log in. Stored in lower case and must be unique.</summary>
    /// <example>ayanda.zulu@example.co.za</example>
    [Required, StringLength(150)]
    [RegularExpression(ContactRules.EmailPattern, ErrorMessage = ContactRules.EmailMessage)]
    public string Email { get; set; } = string.Empty;

    /// <summary>8 to 100 characters with an upper-case letter, a lower-case letter and a digit.</summary>
    /// <example>RaceDay@2026</example>
    [Required]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Must match password.</summary>
    /// <example>RaceDay@2026</example>
    [Required]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <example>0821234567</example>
    [StringLength(20)]
    [RegularExpression(ContactRules.PhonePattern, ErrorMessage = ContactRules.PhoneMessage)]
    public string? PhoneNumber { get; set; }

    /// <summary>yyyy-MM-dd, in the past. Organisers must be at least 18.</summary>
    /// <example>1995-04-12</example>
    [Required]
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>M, F or O.</summary>
    /// <example>F</example>
    [Required, OneOf(typeof(Genders))]
    public string Gender { get; set; } = string.Empty;

    /// <summary>Organiser or Participant. Chosen once here and cannot be changed afterwards.</summary>
    /// <example>Participant</example>
    [Required, OneOf(typeof(RoleNames))]
    public string Role { get; set; } = string.Empty;
}

/// <summary>Credentials for logging in.</summary>
public class LoginRequest
{
    /// <example>ayesha.patel@gmail.com</example>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <example>RaceDay@2026</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

/// <summary>A refresh token issued at login or by a previous refresh.</summary>
public class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>A new access token for an existing session.</summary>
public class TokenResponse
{
    /// <summary>The JWT. Send it as <c>Authorization: Bearer {token}</c>.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>When the access token stops working (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Single use: exchange it at POST /api/auth/refresh for a new token.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>When the session ends unless it is refreshed (UTC).</summary>
    public DateTime SessionExpiresAt { get; set; }
}

/// <summary>The result of a successful login.</summary>
public class LoginResponse : TokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyOrder(1)]
    public UserProfileDto User { get; set; } = new();
}
