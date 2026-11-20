using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.Areas.Organiser.Models;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class DashboardController(IRaceDayApi api, TimeProvider clock) : OrganiserController(api)
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var events = await ApiPaging.CollectAllAsync(page =>
            Api.GetMyEventsAsync(status: null, page, ApiPaging.MaxPageSize, cancellationToken));

        return View(new DashboardViewModel(CurrentUser.FirstName, events, clock.Today()));
    }
}
