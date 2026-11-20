using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Areas.Participant.Models;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class ResultsController(IRaceDayApi api) : ParticipantController(api)
{
    private const int PageSize = 15;

    [HttpGet]
    public async Task<IActionResult> Index(string? discipline, int page = 1, CancellationToken cancellationToken = default)
    {
        discipline = Disciplines.All.FirstOrDefault(d => d == discipline);
        var results = (await Api.GetMyResultsAsync(discipline, Math.Max(page, 1), PageSize, cancellationToken)).Require();
        var all = await ApiPaging.CollectAllAsync(p => Api.GetMyResultsAsync(null, p, ApiPaging.MaxPageSize, cancellationToken));

        return View(new MyResultsViewModel { Discipline = discipline, Results = results, AllResults = all });
    }
}
