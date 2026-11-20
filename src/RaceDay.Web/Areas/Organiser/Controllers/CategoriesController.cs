using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.Areas.Organiser.Models;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Areas.Organiser.Controllers;

public class CategoriesController(IRaceDayApi api) : OrganiserController(api)
{
    [HttpGet]
    public async Task<IActionResult> Index(int eventId, CancellationToken cancellationToken) =>
        View(new CategoriesViewModel { Event = await LoadOwnEventAsync(eventId, cancellationToken) });

    [HttpPost]
    public async Task<IActionResult> Create(int eventId, [Bind(Prefix = "NewCategory")] CategoryInput input, CancellationToken cancellationToken)
    {
        var ev = await LoadOwnEventAsync(eventId, cancellationToken);
        if (ModelState.IsValid)
        {
            var created = await Api.CreateCategoryAsync(eventId, input.ToRequest(), cancellationToken);
            if (created.Succeeded && created.Value is not null)
            {
                FlashSuccess($"{Display.Distance(created.Value.DistanceKm)} \"{created.Value.CategoryName}\" was added.");
                return RedirectToAction(nameof(Index), new { eventId });
            }

            AddErrors(created, "NewCategory.");
        }

        return View(nameof(Index), new CategoriesViewModel { Event = ev, NewCategory = input });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = (await Api.GetCategoryAsync(id, cancellationToken)).Require();
        var ev = await LoadOwnEventAsync(category.EventId, cancellationToken);
        return View(new CategoryEditViewModel { Event = ev, Category = category, Input = CategoryInput.From(category) });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, [Bind(Prefix = "Input")] CategoryInput input, CancellationToken cancellationToken)
    {
        var category = (await Api.GetCategoryAsync(id, cancellationToken)).Require();
        var ev = await LoadOwnEventAsync(category.EventId, cancellationToken);
        if (ModelState.IsValid)
        {
            var updated = await Api.UpdateCategoryAsync(id, input.ToRequest(), cancellationToken);
            if (updated.Succeeded)
            {
                FlashSuccess($"\"{input.CategoryName}\" was updated.");
                return RedirectToAction(nameof(Index), new { eventId = ev.EventId });
            }

            AddErrors(updated, "Input.");
        }

        return View(new CategoryEditViewModel { Event = ev, Category = category, Input = input });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId, CancellationToken cancellationToken)
    {
        await LoadOwnEventAsync(eventId, cancellationToken);
        var deleted = await Api.DeleteCategoryAsync(id, cancellationToken);
        if (deleted.Succeeded)
        {
            FlashSuccess("The category was removed.");
        }
        else
        {
            FlashError(deleted.ErrorMessage);
        }

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private void AddErrors(ApiResult result, string prefix)
    {
        if (result.FieldErrors.Count == 0)
        {
            ModelState.AddModelError(string.Empty, result.ErrorMessage);
            return;
        }

        foreach (var (key, messages) in result.FieldErrors)
        {
            var property = typeof(CategoryInput).GetProperty(key, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            foreach (var message in messages)
            {
                ModelState.AddModelError(property is null ? string.Empty : prefix + property.Name, message);
            }
        }
    }
}
