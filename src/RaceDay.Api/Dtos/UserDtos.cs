using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Domain;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>A user's profile. The password hash is never returned.</summary>
public class UserProfileDto
{
    /// <example>5</example>
    public int UserId { get; set; }

    /// <summary>Organiser or Participant.</summary>
    /// <example>Participant</example>
    public string Role { get; set; } = string.Empty;

    /// <example>Ayesha</example>
    public string FirstName { get; set; } = string.Empty;

    /// <example>Patel</example>
    public string LastName { get; set; } = string.Empty;

    /// <example>ayesha.patel@gmail.com</example>
    public string Email { get; set; } = string.Empty;

    /// <example>0827418529</example>
    public string? PhoneNumber { get; set; }

    /// <example>1991-09-30</example>
    public DateOnly DateOfBirth { get; set; }

    /// <summary>M, F or O.</summary>
    /// <example>F</example>
    public string Gender { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }

    /// <summary>A read-only link to the profile picture in Azure Blob Storage, or null if there is none.</summary>
    public string? ProfilePictureUrl { get; set; }
}

public static class UserMappings
{
    public static readonly Expression<Func<User, UserProfileDto>> ToProfile = u => new UserProfileDto
    {
        UserId = u.UserId,
        Role = u.Role.RoleName,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email,
        PhoneNumber = u.PhoneNumber,
        DateOfBirth = u.DateOfBirth,
        Gender = u.Gender,
        CreatedAt = u.CreatedAt,
        IsActive = u.IsActive,
        ProfilePictureUrl = u.ProfilePictureUrl
    };

    private static readonly Func<User, UserProfileDto> ToProfileCompiled = ToProfile.Compile();

    public static UserProfileDto ToProfileDto(this User user) => ToProfileCompiled(user);
}

/// <summary>The profile fields a user may change. Email and role are fixed after registration.</summary>
public class UpdateProfileRequest
{
    /// <example>Ayesha</example>
    [Required, StringLength(60)]
    public string FirstName { get; set; } = string.Empty;

    /// <example>Patel-Naidoo</example>
    [Required, StringLength(60)]
    public string LastName { get; set; } = string.Empty;

    /// <example>0827418529</example>
    [StringLength(20)]
    [RegularExpression(ContactRules.PhonePattern, ErrorMessage = ContactRules.PhoneMessage)]
    public string? PhoneNumber { get; set; }

    /// <example>1991-09-30</example>
    [Required]
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>M, F or O.</summary>
    /// <example>F</example>
    [Required, OneOf(typeof(Genders))]
    public string Gender { get; set; } = string.Empty;
}

/// <summary>A password change. The current password must be supplied.</summary>
public class ChangePasswordRequest
{
    /// <example>RaceDay@2026</example>
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <example>NewRaceDay@2027</example>
    [Required]
    [RegularExpression(PasswordRules.Pattern, ErrorMessage = PasswordRules.Message)]
    public string NewPassword { get; set; } = string.Empty;

    /// <example>NewRaceDay@2027</example>
    [Required]
    [Compare(nameof(NewPassword), ErrorMessage = "The new passwords do not match.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

/// <summary>Filters for listing accounts.</summary>
public class UserListQuery : PagingQuery
{
    /// <summary>Only accounts with this role: Organiser or Participant.</summary>
    /// <example>Participant</example>
    [FromQuery(Name = "role")]
    [OneOf(typeof(RoleNames))]
    public string? Role { get; set; }
}
