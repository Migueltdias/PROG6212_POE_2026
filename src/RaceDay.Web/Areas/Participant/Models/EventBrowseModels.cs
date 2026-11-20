using System.ComponentModel.DataAnnotations;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Areas.Participant.Models;

public sealed class EventFilter
{
    [StringLength(100)]
    [Display(Name = "Search")]
    public string? Search { get; set; }

    public string? Province { get; set; }

    [Display(Name = "Event type")]
    public string? Discipline { get; set; }

    [Display(Name = "From")]
    public DateOnly? FromDate { get; set; }

    [Display(Name = "To")]
    public DateOnly? ToDate { get; set; }

    public int Page { get; set; } = 1;

    public bool IsFiltered =>
        !string.IsNullOrWhiteSpace(Search) || !string.IsNullOrWhiteSpace(Province) || !string.IsNullOrWhiteSpace(Discipline)
        || FromDate is not null || ToDate is not null;
}

public sealed class EventBrowseViewModel
{
    public EventFilter Filter { get; init; } = new();
    public PagedResult<EventSummary> Events { get; init; } = new();
    public string? Error { get; init; }
    public string FirstName { get; init; } = string.Empty;
}

public sealed class EventDetailsViewModel
{
    public EventDetail Event { get; init; } = new();
    public IReadOnlyList<Enrolment> MyEntries { get; init; } = [];
    public WeatherSnapshot? Weather { get; init; }
    public DateOnly Today { get; init; }

    public string EntryState =>
        Event.EventStatus != EventStatuses.Published || Today > Event.RegistrationCloses ? "Closed"
        : Today < Event.RegistrationOpens ? "Soon"
        : "Open";

    public bool EntriesOpen => EntryState == "Open";

    public Enrolment? ActiveEntryFor(int categoryId) =>
        MyEntries.FirstOrDefault(e => e.CategoryId == categoryId && e.IsActive);
}
