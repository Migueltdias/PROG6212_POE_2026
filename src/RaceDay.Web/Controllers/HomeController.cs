using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;

namespace RaceDay.Web.Controllers;

public class HomeController(IRaceDayApi api, TimeProvider clock, ILogger<HomeController> logger) : AppController
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (SignedInUser is { } user)
        {
            return RedirectToRoleHome(user.Role);
        }

        try
        {
            var upcoming = await api.BrowseEventsAsync(new EventQuery { FromDate = clock.Today(), PageSize = 6 }, cancellationToken);
            return View(new HomeViewModel
            {
                UpcomingEvents = upcoming.Value?.Items ?? [],
                UpcomingCount = upcoming.Value?.TotalCount ?? 0,
                ApiAvailable = upcoming.Succeeded
            });
        }
        catch (ApiUnavailableException)
        {
            return View(new HomeViewModel { ApiAvailable = false });
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public IActionResult Error()
    {
        var feature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        if (feature?.Error is { } error)
        {
            logger.LogError(error, "Unhandled exception on {Path}", feature.Path);
        }

        var problem = feature?.Error is ApiUnavailableException ? ProblemViewModel.Unavailable() : ProblemViewModel.Error();
        Response.StatusCode = problem.StatusCode;
        return View("Problem", problem with { RequestId = HttpContext.TraceIdentifier });
    }

    [Route("Home/Status/{code:int}")]
    [IgnoreAntiforgeryToken]
    public IActionResult Status(int code)
    {
        Response.StatusCode = code;
        return View("Problem", ProblemViewModel.ForStatus(code));
    }
}
