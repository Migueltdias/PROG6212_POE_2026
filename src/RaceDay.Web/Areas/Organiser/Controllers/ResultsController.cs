using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Areas.Organiser.Models;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class ResultsController(IRaceDayApi api, TimeProvider clock) : OrganiserController(api)
{
    [HttpGet]
    public async Task<IActionResult> Index(int eventId, CancellationToken cancellationToken)
    {
        var ev = await LoadOwnEventAsync(eventId, cancellationToken);
        var summaries = new List<CategoryResultsSummary>();

        if (ev.EventStatus != EventStatuses.Draft)
        {
            var results = (await Api.GetEventResultsAsync(eventId, cancellationToken)).Require();
            foreach (var category in ev.Categories)
            {
                var confirmed = (await Api.GetCategoryEnrolmentsAsync(category.CategoryId, EnrolmentStatuses.Confirmed, 1, 1, cancellationToken)).Require();
                var rows = results.Categories.FirstOrDefault(c => c.CategoryId == category.CategoryId)?.Results ?? [];
                summaries.Add(new CategoryResultsSummary(category, confirmed.TotalCount, rows));
            }
        }

        return View(new ResultsOverviewViewModel { Event = ev, Categories = summaries, Today = clock.Today() });
    }

    [HttpGet]
    public async Task<IActionResult> Capture(int categoryId, CancellationToken cancellationToken)
    {
        var category = (await Api.GetCategoryAsync(categoryId, cancellationToken)).Require();
        var ev = await LoadOwnEventAsync(category.EventId, cancellationToken);

        var entries = await ApiPaging.CollectAllAsync(page =>
            Api.GetCategoryEnrolmentsAsync(categoryId, EnrolmentStatuses.Confirmed, page, ApiPaging.MaxPageSize, cancellationToken));
        var results = ev.EventStatus == EventStatuses.Draft
            ? []
            : await ApiPaging.CollectAllAsync(page => Api.GetLeaderboardAsync(categoryId, page, ApiPaging.MaxPageSize, cancellationToken));
        var byEntry = results.ToDictionary(r => r.EnrolmentId);

        var rows = entries
            .OrderBy(e => byEntry.TryGetValue(e.EnrolmentId, out var r) ? r.OverallPosition ?? int.MaxValue : int.MaxValue)
            .ThenBy(e => e.RaceNumber, StringComparer.OrdinalIgnoreCase)
            .Select(e => new CaptureRow
            {
                EnrolmentId = e.EnrolmentId,
                RaceNumber = e.RaceNumber,
                ParticipantName = e.ParticipantName,
                Existing = byEntry.GetValueOrDefault(e.EnrolmentId)
            })
            .ToList();

        return View(new CaptureViewModel { Event = ev, Category = category, Rows = rows, Today = clock.Today() });
    }

    [HttpPost]
    public async Task<IActionResult> Capture(int categoryId, List<CaptureInput> rows, CancellationToken cancellationToken)
    {
        var category = (await Api.GetCategoryAsync(categoryId, cancellationToken)).Require();
        await LoadOwnEventAsync(category.EventId, cancellationToken);

        var toSend = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.ResultStatus) && ResultStatuses.All.Contains(r.ResultStatus))
            .Select(r => new BulkResultRow
            {
                RaceNumber = r.RaceNumber,
                ResultStatus = r.ResultStatus!,
                FinishTime = r.ResultStatus == ResultStatuses.Finished ? r.FinishTime : null
            })
            .ToList();

        if (toSend.Count == 0)
        {
            FlashInfo("Choose a result for at least one runner, and a finish time for each finisher.");
            return RedirectToAction(nameof(Capture), new { categoryId });
        }

        var imported = await Api.ImportResultsAsync(categoryId, new BulkResultsRequest { Results = toSend }, cancellationToken);
        if (!imported.Succeeded || imported.Value is null)
        {
            FlashError(imported.ErrorMessage);
            return RedirectToAction(nameof(Capture), new { categoryId });
        }

        var outcome = imported.Value;
        if (outcome.Imported > 0)
        {
            FlashSuccess($"Captured {outcome.Imported} {(outcome.Imported == 1 ? "result" : "results")}. Overall, gender and age-group positions were worked out from the finish times.");
        }

        if (outcome.Errors.Count > 0)
        {
            FlashWarning($"{outcome.Errors.Count} {(outcome.Errors.Count == 1 ? "row was" : "rows were")} not saved: "
                         + string.Join("; ", outcome.Errors.Select(e => $"{e.RaceNumber}: {e.Error}")));
        }
        else if (outcome.Imported == 0)
        {
            FlashInfo("Those runners already had results, so nothing changed.");
        }

        return RedirectToAction(nameof(Capture), new { categoryId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = (await Api.GetResultAsync(id, cancellationToken)).Require();
        await LoadOwnEventAsync(result.EventId, cancellationToken);
        return View(ResultEditViewModel.From(result));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ResultEditViewModel model, CancellationToken cancellationToken)
    {
        var result = (await Api.GetResultAsync(id, cancellationToken)).Require();
        await LoadOwnEventAsync(result.EventId, cancellationToken);
        model.ResultId = id;
        model.Result = result;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await Api.UpdateResultAsync(id, model.ToRequest(), cancellationToken);
        if (!updated.Succeeded)
        {
            ModelState.AddApiErrors(updated, typeof(ResultEditViewModel));
            return View(model);
        }

        FlashSuccess($"The result for {result.ParticipantName} ({result.RaceNumber}) was corrected.");
        return RedirectToAction(nameof(Capture), new { categoryId = result.CategoryId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = (await Api.GetResultAsync(id, cancellationToken)).Require();
        await LoadOwnEventAsync(result.EventId, cancellationToken);

        var deleted = await Api.DeleteResultAsync(id, cancellationToken);
        if (deleted.Succeeded)
        {
            FlashSuccess($"The result for {result.ParticipantName} ({result.RaceNumber}) was removed and can be captured again.");
        }
        else
        {
            FlashError(deleted.ErrorMessage);
        }

        return RedirectToAction(nameof(Capture), new { categoryId = result.CategoryId });
    }

    [HttpPost]
    public async Task<IActionResult> Publish(int eventId, CancellationToken cancellationToken)
    {
        await LoadOwnEventAsync(eventId, cancellationToken);
        var completed = await Api.ChangeEventStatusAsync(eventId, EventStatuses.Completed, cancellationToken);
        if (completed.Succeeded)
        {
            FlashSuccess("Results published. The event is now complete, and participants can see their finish times and positions.");
        }
        else
        {
            FlashError(completed.ErrorMessage);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }
}
