using System.ComponentModel.DataAnnotations;
using System.Reflection;
using RaceDay.Api.Domain;

namespace RaceDay.Api.Infrastructure;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class OneOfAttribute : ValidationAttribute
{
    public OneOfAttribute(Type valuesType)
    {
        var field = valuesType.GetField("All", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new ArgumentException($"{valuesType.Name} has no public static All field.");
        Allowed = (string[])field.GetValue(null)!;
    }

    public string[] Allowed { get; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || value is string text && string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success;
        }

        if (value is string candidate && DomainValue.Canonical(candidate, Allowed) is not null)
        {
            return ValidationResult.Success;
        }

        var memberNames = validationContext.MemberName is null ? null : new[] { validationContext.MemberName };
        return new ValidationResult(
            ErrorMessage ?? $"{validationContext.DisplayName} must be one of: {string.Join(", ", Allowed)}.",
            memberNames);
    }
}
