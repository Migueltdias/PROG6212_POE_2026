using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Areas.Participant.Models;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class EventsController(IRaceDayApi api, TimeProvider clock) : ParticipantController(api)
{
    private const int PageSize = 9;

    [HttpGet]
    public async Task<IActionResult> Index(EventFilter filter, CancellationToken cancellationToken)
    {
        var today = clock.Today();
        var query = new EventQuery
        {
            Search = filter.Search,
            Province = Provinces.All.FirstOrDefault(p => p == filter.Province),
            Discipline = Disciplines.All.FirstOrDefault(d => d == filter.Discipline),
            FromDate = filter.FromDate is { } from && from > today ? from : today,
            ToDate = filter.ToDate,
            Page = Math.Max(filter.Page, 1),
            PageSize = PageSize
        };

        var result = await Api.BrowseEventsAsync(query, cancellationToken);
        return View(new EventBrowseViewModel
        {
            Filter = filter,
            Events = result.Value ?? new PagedResult<EventSummary>(),
            Error = result.Succeeded ? null : result.ErrorMessage,
            FirstName = CurrentUser.FirstName
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var ev = (await Api.GetEventAsync(id, cancellationToken)).Require();
        var myEntries = (await LoadAllMyEntriesAsync(cancellationToken)).Where(e => e.EventId == id).ToList();
        var weather = await Api.GetEventWeatherAsync(id, cancellationToken);

        return View(new EventDetailsViewModel
        {
            Event = ev,
            MyEntries = myEntries,
            Weather = weather.Succeeded ? weather.Value : null,
            Today = clock.Today()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Results(int id, CancellationToken cancellationToken)
    {
        var results = (await Api.GetEventResultsAsync(id, cancellationToken)).Require();
        ViewData["MyUserId"] = CurrentUser.UserId;
        return View(results);
    }
}
