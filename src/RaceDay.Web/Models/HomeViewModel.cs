using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Models;

public sealed class HomeViewModel
{
    public IReadOnlyList<EventSummary> UpcomingEvents { get; init; } = [];

    public int UpcomingCount { get; init; }

    public bool ApiAvailable { get; init; } = true;
}
