using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.Areas.Participant.Models;

public sealed class MyResultsViewModel
{
    public string? Discipline { get; init; }
    public PagedResult<RaceResult> Results { get; init; } = new();
    public IReadOnlyList<RaceResult> AllResults { get; init; } = [];

    private IEnumerable<RaceResult> Finished => AllResults.Where(r => r.ResultStatus == ResultStatuses.Finished);

    public int RacesFinished => Finished.Count();
    public decimal KilometresRaced => Finished.Sum(r => r.DistanceKm);
    public int? BestPosition => Finished.Where(r => r.OverallPosition is not null).Min(r => r.OverallPosition);
    public RaceResult? LongestRace => Finished.MaxBy(r => r.DistanceKm);
}
