using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Areas.Organiser.Models;

public sealed class CategoriesViewModel
{
    public EventDetail Event { get; init; } = new();

    public CategoryInput NewCategory { get; init; } = CategoryInput.Default();

    public bool CanChange => Event.EventStatus is not (EventStatuses.Cancelled or EventStatuses.Completed);
}

public sealed class CategoryEditViewModel
{
    public EventDetail Event { get; init; } = new();

    public Category Category { get; init; } = new();

    public CategoryInput Input { get; init; } = new();
}
