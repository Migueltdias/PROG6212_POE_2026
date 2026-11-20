using RaceDay.Api.Domain;
using RaceDay.Api.Infrastructure;

namespace RaceDay.Api.Services;

public static class UserRules
{
    public const int MinimumOrganiserAge = 18;

    public static string NormaliseEmail(string email) => email.Trim().ToLowerInvariant();

    public static string? NormaliseOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static ServiceError? ValidateDateOfBirth(DateOnly dateOfBirth, string role, DateOnly today)
    {
        if (dateOfBirth >= today)
        {
            return Errors.Validation("dateOfBirth", "Date of birth must be in the past.");
        }

        if (dateOfBirth < today.AddYears(-120))
        {
            return Errors.Validation("dateOfBirth", "Date of birth is more than 120 years ago.");
        }

        if (role == RoleNames.Organiser && TimeProviderExtensions.AgeOn(dateOfBirth, today) < MinimumOrganiserAge)
        {
            return Errors.Validation("dateOfBirth", $"Organisers must be at least {MinimumOrganiserAge} years old.");
        }

        return null;
    }
}
