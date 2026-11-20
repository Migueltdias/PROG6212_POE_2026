using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Areas.Organiser.Models;

public sealed class DashboardViewModel
{
    public DashboardViewModel(string firstName, IReadOnlyList<EventSummary> events, DateOnly today)
    {
        FirstName = firstName;
        Today = today;
        Events = events.OrderBy(e => e.EventDate < today).ThenBy(e => e.EventDate < today ? -e.EventDate.DayNumber : e.EventDate.DayNumber).ToList();
        Upcoming = events
            .Where(e => e.EventDate >= today && e.EventStatus is EventStatuses.Published or EventStatuses.Draft)
            .OrderBy(e => e.EventDate)
            .ToList();
        AwaitingResults = events
            .Where(e => e.EventStatus == EventStatuses.Published && e.EventDate <= today)
            .OrderBy(e => e.EventDate)
            .ToList();
    }

    public string FirstName { get; }
    public DateOnly Today { get; }
    public IReadOnlyList<EventSummary> Events { get; }
    public IReadOnlyList<EventSummary> Upcoming { get; }
    public IReadOnlyList<EventSummary> AwaitingResults { get; }

    public EventSummary? NextEvent => Upcoming.FirstOrDefault(e => e.EventStatus == EventStatuses.Published) ?? Upcoming.FirstOrDefault();
    public int TotalEntries => Events.Sum(e => e.EnteredCount);
    public int ConfirmedEntries => Events.Sum(e => e.ConfirmedCount);
    public int PendingEntries => Events.Sum(e => e.PendingCount);
    public int CountWithStatus(string status) => Events.Count(e => e.EventStatus == status);
}
