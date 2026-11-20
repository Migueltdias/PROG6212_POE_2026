using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Areas.Organiser.Models;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class EventsController(IRaceDayApi api, TimeProvider clock) : OrganiserController(api)
{
    private const int PageSize = 10;

    private static readonly Dictionary<string, string> CreateErrorKeys = new()
    {
        ["categories[0]."] = "FirstCategory.",
        ["categories"] = "FirstCategory.CategoryName"
    };

    [HttpGet]
    public async Task<IActionResult> Index(string? status, int page = 1, CancellationToken cancellationToken = default)
    {
        status = EventStatuses.All.FirstOrDefault(s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));
        var events = (await Api.GetMyEventsAsync(status, Math.Max(page, 1), PageSize, cancellationToken)).Require();
        return View(new EventListViewModel(events, status, clock.Today()));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken) =>
        View(new EventDetailsViewModel(await LoadOwnEventAsync(id, cancellationToken), clock.Today()));

    [HttpGet]
    public IActionResult Create() => View("Form", EventFormViewModel.ForNewEvent(clock.Today()));

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model, CancellationToken cancellationToken)
    {
        model.EventId = null;
        if (model.FirstCategory is null)
        {
            ModelState.AddModelError("FirstCategory.CategoryName", "Add the event's first distance and event type.");
        }

        ValidateBanner(model.Banner);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var created = await Api.CreateEventAsync(model.ToCreateRequest(), cancellationToken);
        if (!created.Succeeded || created.Value is null)
        {
            ModelState.AddApiErrors(created, typeof(EventFormViewModel), CreateErrorKeys);
            return View("Form", model);
        }

        var bannerError = await UploadBannerAsync(created.Value.EventId, model.Banner, cancellationToken);
        if (bannerError is null)
        {
            FlashSuccess($"\"{created.Value.EventName}\" was created as a draft. Add any other categories, then publish it to open entries.");
        }
        else
        {
            FlashWarning($"\"{created.Value.EventName}\" was created, but the banner was not uploaded: {bannerError} You can add it from Edit.");
        }

        return RedirectToAction(nameof(Details), new { id = created.Value.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var ev = await LoadOwnEventAsync(id, cancellationToken);
        if (ev.EventStatus is EventStatuses.Cancelled or EventStatuses.Completed)
        {
            FlashInfo($"This event is {ev.EventStatus.ToLowerInvariant()}, so it can no longer be edited.");
            return RedirectToAction(nameof(Details), new { id });
        }

        return View("Form", EventFormViewModel.From(ev));
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model, CancellationToken cancellationToken)
    {
        var ev = await LoadOwnEventAsync(id, cancellationToken);
        model.WithContext(ev);
        model.FirstCategory = null;
        foreach (var key in ModelState.Keys.Where(k => k.StartsWith(nameof(model.FirstCategory), StringComparison.Ordinal)).ToList())
        {
            ModelState.Remove(key);
        }

        ValidateBanner(model.Banner);
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        var updated = await Api.UpdateEventAsync(id, model.ToRequest(), cancellationToken);
        if (!updated.Succeeded)
        {
            ModelState.AddApiErrors(updated, typeof(EventFormViewModel));
            return View("Form", model);
        }

        string? bannerError = null;
        if (model.Banner is { Length: > 0 })
        {
            bannerError = await UploadBannerAsync(id, model.Banner, cancellationToken);
        }
        else if (model.RemoveBanner && ev.BannerImageUrl is not null)
        {
            var removed = await Api.RemoveEventBannerAsync(id, cancellationToken);
            bannerError = removed.Succeeded ? null : removed.ErrorMessage;
        }

        if (bannerError is null)
        {
            FlashSuccess("Your changes have been saved.");
        }
        else
        {
            FlashWarning($"Your changes were saved, but the banner was not updated: {bannerError}");
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Status(int id, string status, CancellationToken cancellationToken)
    {
        var result = await Api.ChangeEventStatusAsync(id, status, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            FlashError(result.ErrorMessage);
        }
        else
        {
            FlashSuccess(result.Value.EventStatus switch
            {
                EventStatuses.Published => $"\"{result.Value.EventName}\" is live. Participants can now find it and enter.",
                EventStatuses.Cancelled => $"\"{result.Value.EventName}\" has been cancelled. Existing entries are kept on record.",
                EventStatuses.Completed => $"\"{result.Value.EventName}\" is complete and its results are published.",
                _ => "The event status was updated."
            });
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        View(await LoadOwnEventAsync(id, cancellationToken));

    [HttpPost, ActionName(nameof(Delete))]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var ev = await LoadOwnEventAsync(id, cancellationToken);
        var result = await Api.DeleteEventAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            FlashError(result.ErrorMessage);
            return RedirectToAction(nameof(Details), new { id });
        }

        FlashSuccess($"\"{ev.EventName}\" was deleted.");
        return RedirectToAction(nameof(Index));
    }

    private void ValidateBanner(IFormFile? banner)
    {
        if (banner is null)
        {
            return;
        }

        if (banner.Length == 0)
        {
            ModelState.AddModelError(nameof(EventFormViewModel.Banner), "The chosen file is empty.");
        }
        else if (banner.Length > EventFormViewModel.MaxBannerBytes)
        {
            ModelState.AddModelError(nameof(EventFormViewModel.Banner), "The banner must be 5 MB or smaller.");
        }
        else if (!banner.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(EventFormViewModel.Banner), "Choose a JPEG, PNG, GIF or WebP image.");
        }
    }

    private async Task<string?> UploadBannerAsync(int eventId, IFormFile? banner, CancellationToken cancellationToken)
    {
        if (banner is not { Length: > 0 })
        {
            return null;
        }

        await using var image = banner.OpenReadStream();
        var result = await Api.UploadEventBannerAsync(eventId, image, banner.FileName, banner.ContentType, cancellationToken);
        return result.Succeeded ? null : result.ErrorMessage;
    }
}
