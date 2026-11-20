using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Session;

namespace RaceDay.Web.Controllers;

[RequireLogin]
public class ProfileController(IRaceDayApi api) : AppController
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var profile = (await api.GetMyProfileAsync(cancellationToken)).Require();
        RefreshSession(profile);
        return View(new ProfilePageViewModel { Profile = profile, Details = ProfileDetailsInput.From(profile) });
    }

    [HttpPost]
    public async Task<IActionResult> Update([Bind(Prefix = nameof(ProfilePageViewModel.Details))] ProfileDetailsInput details, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var updated = await api.UpdateMyProfileAsync(details.ToRequest(), cancellationToken);
            if (updated.Succeeded && updated.Value is not null)
            {
                RefreshSession(updated.Value);
                FlashSuccess("Your profile was updated.");
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddApiErrors(updated, typeof(ProfileDetailsInput), prefix: "Details.");
        }

        var profile = (await api.GetMyProfileAsync(cancellationToken)).Require();
        return View(nameof(Index), new ProfilePageViewModel { Profile = profile, Details = details });
    }

    [HttpPost]
    public async Task<IActionResult> Picture(IFormFile? picture, CancellationToken cancellationToken)
    {
        if (picture is not { Length: > 0 })
        {
            FlashError("Choose a picture to upload.");
            return RedirectToAction(nameof(Index));
        }

        if (picture.Length > ProfilePageViewModel.MaxPictureBytes || !picture.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            FlashError("Choose a JPEG, PNG, GIF or WebP image of 5 MB or less.");
            return RedirectToAction(nameof(Index));
        }

        await using var image = picture.OpenReadStream();
        var uploaded = await api.UploadProfilePictureAsync(image, picture.FileName, picture.ContentType, cancellationToken);
        if (uploaded.Succeeded && uploaded.Value is not null)
        {
            RefreshSession(uploaded.Value);
            FlashSuccess("Looking good! Your new profile picture is saved.");
        }
        else
        {
            FlashError($"Your picture was not saved: {uploaded.ErrorMessage}");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RemovePicture(CancellationToken cancellationToken)
    {
        var removed = await api.RemoveProfilePictureAsync(cancellationToken);
        if (removed.Succeeded)
        {
            var user = CurrentUser;
            user.ProfilePictureUrl = null;
            HttpContext.Session.SetUser(user);
            FlashSuccess("Your profile picture was removed.");
        }
        else
        {
            FlashError(removed.ErrorMessage);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Password([Bind(Prefix = nameof(ProfilePageViewModel.Password))] PasswordInput password, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var changed = await api.ChangePasswordAsync(new ChangePasswordRequest
            {
                CurrentPassword = password.CurrentPassword,
                NewPassword = password.NewPassword,
                ConfirmNewPassword = password.ConfirmNewPassword
            }, cancellationToken);

            if (changed.Succeeded)
            {
                FlashSuccess("Your password was changed. Any other devices signed in to your account have been signed out.");
                return RedirectToAction(nameof(Index));
            }

            if (changed.StatusCode == HttpStatusCode.Unauthorized)
            {
                ModelState.AddModelError("Password.CurrentPassword", changed.ErrorMessage);
            }
            else
            {
                ModelState.AddApiErrors(changed, typeof(PasswordInput), prefix: "Password.");
            }
        }

        var profile = (await api.GetMyProfileAsync(cancellationToken)).Require();
        return View(nameof(Index), new ProfilePageViewModel { Profile = profile, Details = ProfileDetailsInput.From(profile) });
    }

    private void RefreshSession(UserProfile profile)
    {
        var user = CurrentUser;
        user.Apply(profile);
        HttpContext.Session.SetUser(user);
    }
}
