using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Areas.Participant.Models;
using RaceDay.Web.Infrastructure;

namespace RaceDay.Web.Areas.Participant.Controllers;

public class EnrolmentsController(IRaceDayApi api, TimeProvider clock) : ParticipantController(api)
{
    private const int PageSize = 10;

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "upcoming", int page = 1, CancellationToken cancellationToken = default)
    {
        tab = tab is "past" or "all" ? tab : "upcoming";
        bool? upcoming = tab switch { "upcoming" => true, "past" => false, _ => null };
        var entries = (await Api.GetMyEnrolmentsAsync(null, upcoming, Math.Max(page, 1), PageSize, cancellationToken)).Require();
        return View(new MyEventsViewModel { Tab = tab, Entries = entries, Today = clock.Today() });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int eventId, int? categoryId, CancellationToken cancellationToken)
    {
        var ev = (await Api.GetEventAsync(eventId, cancellationToken)).Require();
        var model = new EnterViewModel { EventId = eventId, CategoryId = categoryId, Event = ev };

        var latest = (await Api.GetMyEnrolmentsAsync(null, null, 1, 1, cancellationToken)).Value?.Items.FirstOrDefault();
        if (latest is not null)
        {
            model.EmergencyContactName = latest.EmergencyContactName;
            model.EmergencyContactPhone = latest.EmergencyContactPhone;
            model.TShirtSize = latest.TShirtSize;
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(EnterViewModel model, CancellationToken cancellationToken)
    {
        model.Event = (await Api.GetEventAsync(model.EventId, cancellationToken)).Require();
        if (model.CategoryId is { } chosen && model.Event.Categories.All(c => c.CategoryId != chosen))
        {
            ModelState.AddModelError(nameof(model.CategoryId), "Choose one of this event's categories.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var entered = await Api.EnterAsync(model.CategoryId!.Value, model.ToRequest(), cancellationToken);
        if (!entered.Succeeded || entered.Value is null)
        {
            if (entered.StatusCode == HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(nameof(model.CategoryId), entered.ErrorMessage);
            }
            else
            {
                ModelState.AddApiErrors(entered, typeof(EnterViewModel));
            }

            return View(model);
        }

        var entry = entered.Value;
        if (entry.EnrolmentStatus == EnrolmentStatuses.Confirmed)
        {
            FlashSuccess($"You're in! {entry.CategoryName} at {entry.EventName} is confirmed. Your race number is {entry.RaceNumber}.");
            return RedirectToAction(nameof(Index));
        }

        FlashSuccess($"You're in! Your race number for {entry.CategoryName} is {entry.RaceNumber}. Pay the entry fee to confirm your place.");
        return RedirectToAction(nameof(Pay), new { id = entry.EnrolmentId });
    }

    [HttpGet]
    public async Task<IActionResult> Pay(int id, CancellationToken cancellationToken)
    {
        var entry = (await Api.GetEnrolmentAsync(id, cancellationToken)).Require();
        if (entry.EnrolmentStatus != EnrolmentStatuses.Pending)
        {
            FlashInfo($"Your entry for {entry.EventName} is {entry.EnrolmentStatus.ToLowerInvariant()}, so there is nothing to pay.");
            return RedirectToAction(nameof(Index));
        }

        return View(entry);
    }

    [HttpPost, ActionName(nameof(Pay))]
    public async Task<IActionResult> PayConfirmed(int id, CancellationToken cancellationToken)
    {
        var entry = (await Api.GetEnrolmentAsync(id, cancellationToken)).Require();

        var reference = $"RD-{clock.GetUtcNow():yyyyMMddHHmmss}-{id.ToString(CultureInfo.InvariantCulture)}";
        var paid = await Api.PayAsync(id, new PaymentRequest { AmountPaid = entry.EntryFee, PaymentReference = reference }, cancellationToken);
        if (!paid.Succeeded)
        {
            FlashError(paid.ErrorMessage);
            return RedirectToAction(nameof(Index));
        }

        FlashSuccess($"Payment of {Display.Money(entry.EntryFee)} received (reference {reference}). Your entry is confirmed. See you on race day!");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var entry = (await Api.GetEnrolmentAsync(id, cancellationToken)).Require();
        return View(new EditEntryViewModel
        {
            Enrolment = entry,
            TShirtSize = entry.TShirtSize,
            EmergencyContactName = entry.EmergencyContactName,
            EmergencyContactPhone = entry.EmergencyContactPhone
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EditEntryViewModel model, CancellationToken cancellationToken)
    {
        model.Enrolment = (await Api.GetEnrolmentAsync(id, cancellationToken)).Require();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var updated = await Api.UpdateEnrolmentAsync(id, model.ToRequest(), cancellationToken);
        if (!updated.Succeeded)
        {
            ModelState.AddApiErrors(updated, typeof(EditEntryViewModel));
            return View(model);
        }

        FlashSuccess("Your entry details were updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Withdraw(int id, CancellationToken cancellationToken)
    {
        var withdrawn = await Api.WithdrawAsync(id, cancellationToken);
        if (withdrawn.Succeeded)
        {
            FlashSuccess("You have withdrawn. If you change your mind, enter the same category again and you keep your race number.");
        }
        else
        {
            FlashError(withdrawn.ErrorMessage);
        }

        return RedirectToAction(nameof(Index));
    }
}
