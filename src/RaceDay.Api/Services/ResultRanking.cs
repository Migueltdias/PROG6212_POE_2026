using RaceDay.Api.Domain;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public static class ResultRanking
{
    public sealed record Entrant(Result Result, string Gender, DateOnly DateOfBirth);

    public static string AgeGroupFor(int age) => age switch
    {
        < 20 => "U20",
        >= 60 => "60+",
        _ => $"{age / 10 * 10}-{age / 10 * 10 + 9}"
    };

    public static void Recalculate(IReadOnlyCollection<Entrant> entrants, DateOnly eventDate)
    {
        var finishers = entrants
            .Where(e => e.Result.ResultStatus == ResultStatuses.Finished && e.Result.ChipTimeSeconds.HasValue)
            .OrderBy(e => e.Result.ChipTimeSeconds)
            .ThenBy(e => e.Result.EnrolmentId)
            .ToList();

        for (var i = 0; i < finishers.Count; i++)
        {
            finishers[i].Result.OverallPosition = i + 1;
        }

        foreach (var group in finishers.GroupBy(e => e.Gender))
        {
            var position = 1;
            foreach (var entrant in group)
            {
                entrant.Result.GenderPosition = position++;
            }
        }

        foreach (var group in finishers.GroupBy(e => (e.Gender, AgeGroupFor(TimeProviderExtensions.AgeOn(e.DateOfBirth, eventDate)))))
        {
            var position = 1;
            foreach (var entrant in group)
            {
                entrant.Result.AgeGroupPosition = position++;
            }
        }

        foreach (var entrant in entrants.Except(finishers))
        {
            entrant.Result.OverallPosition = null;
            entrant.Result.GenderPosition = null;
            entrant.Result.AgeGroupPosition = null;
        }
    }
}
