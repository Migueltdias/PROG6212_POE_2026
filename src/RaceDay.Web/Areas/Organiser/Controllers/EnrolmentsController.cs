using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Areas.Organiser.Models;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class EnrolmentsController(IRaceDayApi api) : OrganiserController(api)
{
    private const int PageSize = 25;

    [HttpGet]
    public async Task<IActionResult> Index(int eventId, int? categoryId, string? status, int page = 1, CancellationToken cancellationToken = default)
    {
        var ev = await LoadOwnEventAsync(eventId, cancellationToken);
        categoryId = ev.Categories.Any(c => c.CategoryId == categoryId) ? categoryId : null;
        status = EnrolmentStatuses.All.FirstOrDefault(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));

        var entries = (await Api.GetEventEnrolmentsAsync(eventId, categoryId, status, Math.Max(page, 1), PageSize, cancellationToken)).Require();

        var counts = new Dictionary<string, int>();
        foreach (var each in EnrolmentStatuses.All)
        {
            var count = (await Api.GetEventEnrolmentsAsync(eventId, categoryId, each, 1, 1, cancellationToken)).Require();
            counts[each] = count.TotalCount;
        }

        return View(new EnrolmentListViewModel
        {
            Event = ev,
            Entries = entries,
            CategoryId = categoryId,
            Status = status,
            Counts = counts
        });
    }
}
