using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Areas.Organiser.Models;

public sealed class EnrolmentListViewModel
{
    public EventDetail Event { get; init; } = new();

    public PagedResult<Enrolment> Entries { get; init; } = new();

    public int? CategoryId { get; init; }

    public string? Status { get; init; }

    public IReadOnlyDictionary<string, int> Counts { get; init; } = new Dictionary<string, int>();

    public int TotalCount => Counts.Values.Sum();
}
