namespace RaceDay.Web.ApiClient.Contracts;

public sealed class UserProfile
{
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public string? ProfilePictureUrl { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}

public sealed class UpdateProfileRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
