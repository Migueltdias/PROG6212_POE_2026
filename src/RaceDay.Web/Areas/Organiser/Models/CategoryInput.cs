using System.ComponentModel.DataAnnotations;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Areas.Organiser.Models;

public sealed class CategoryInput
{
    [Required(ErrorMessage = "Name the category, for example \"21.1km Half Marathon\".")]
    [StringLength(80)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Choose the event type.")]
    [Display(Name = "Event type")]
    public string Discipline { get; set; } = "Run";

    [Required(ErrorMessage = "Enter the distance.")]
    [Range(0.01, 9999.99, ErrorMessage = "The distance must be between 0.01 and 9 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Enter the entry fee (0 for a free category).")]
    [Range(0, 99999999.99, ErrorMessage = "The entry fee cannot be negative.")]
    [Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; }

    [Required(ErrorMessage = "Enter the field limit.")]
    [Range(1, 1_000_000, ErrorMessage = "The field limit must be at least 1.")]
    [Display(Name = "Field limit")]
    public int? MaxParticipants { get; set; }

    [Required(ErrorMessage = "Enter the start time.")]
    [Display(Name = "Start time")]
    public TimeOnly? StartTime { get; set; }

    [Range(0, 100, ErrorMessage = "The minimum age must be between 0 and 100.")]
    [Display(Name = "Minimum age")]
    public int? MinimumAge { get; set; } = 12;

    public static CategoryInput Default(TimeOnly? startTime = null) => new()
    {
        Discipline = "Run",
        MaxParticipants = 500,
        EntryFee = 150m,
        StartTime = startTime ?? new TimeOnly(6, 0),
        MinimumAge = 12
    };

    public static CategoryInput From(Category category) => new()
    {
        CategoryName = category.CategoryName,
        Discipline = category.Discipline,
        DistanceKm = category.DistanceKm,
        EntryFee = category.EntryFee,
        MaxParticipants = category.MaxParticipants,
        StartTime = category.StartTime,
        MinimumAge = category.MinimumAge
    };

    public CategoryRequest ToRequest() => new()
    {
        CategoryName = CategoryName.Trim(),
        Discipline = Discipline,
        DistanceKm = DistanceKm,
        EntryFee = EntryFee,
        MaxParticipants = MaxParticipants,
        StartTime = StartTime,
        MinimumAge = MinimumAge
    };
}
