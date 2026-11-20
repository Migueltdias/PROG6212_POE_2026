using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.ApiClient;
using RaceDay.Web.ApiClient.Contracts;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Models;
using RaceDay.Web.Session;

namespace RaceDay.Web.Controllers;

public class AccountController(IRaceDayApi api, ILogger<AccountController> logger) : AppController
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (SignedInUser is { } user)
        {
            return RedirectToRoleHome(user.Role);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await api.LoginAsync(new LoginRequest { Email = model.Email.Trim(), Password = model.Password }, cancellationToken);
        if (!result.Succeeded || result.Value is null)
        {
            ModelState.AddModelError(string.Empty, result.StatusCode == HttpStatusCode.Unauthorized
                ? result.ErrorMessage
                : "We could not sign you in just now. Please try again.");
            model.Password = string.Empty;
            return View(model);
        }

        var user = StartSession(result.Value);
        FlashSuccess($"Welcome back, {user.FirstName}!");

        return IsReturnUrlFor(model.ReturnUrl, user.Role)
            ? LocalRedirect(model.ReturnUrl!)
            : RedirectToRoleHome(user.Role);
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (SignedInUser is { } user)
        {
            return RedirectToRoleHome(user.Role);
        }

        return View(new RegisterViewModel { Role = Roles.Participant });
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(ClearPasswords(model));
        }

        var registered = await api.RegisterAsync(new RegisterRequest
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email.Trim(),
            Password = model.Password,
            ConfirmPassword = model.ConfirmPassword,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            DateOfBirth = model.DateOfBirth,
            Gender = model.Gender,
            Role = model.Role
        }, cancellationToken);

        if (!registered.Succeeded)
        {
            if (registered.StatusCode == HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(nameof(model.Email), registered.ErrorMessage);
            }
            else
            {
                ModelState.AddApiErrors(registered, typeof(RegisterViewModel));
            }

            return View(ClearPasswords(model));
        }

        var login = await api.LoginAsync(new LoginRequest { Email = model.Email.Trim(), Password = model.Password }, cancellationToken);
        if (!login.Succeeded || login.Value is null)
        {
            FlashSuccess("Your account is ready. Sign in to continue.");
            return RedirectToAction(nameof(Login));
        }

        var user = StartSession(login.Value);
        FlashSuccess(user.IsOrganiser
            ? $"Welcome to RaceDay, {user.FirstName}! Create your first event to get started."
            : $"Welcome to RaceDay, {user.FirstName}! Find an event and enter in a few clicks.");
        return RedirectToRoleHome(user.Role);
    }

    [HttpPost]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (SignedInUser is not null)
        {
            try
            {
                await api.LogoutAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is ApiUnavailableException or ApiSessionExpiredException)
            {
                logger.LogInformation("The API session could not be revoked during logout: {Message}", ex.Message);
            }
        }

        HttpContext.Session.Clear();
        Response.Cookies.Delete(SessionSetup.CookieName);
        FlashSuccess("You have signed out. See you at the start line!");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View(SignedInUser);
    }

    private SessionUser StartSession(LoginResponse login)
    {
        var user = SessionUser.From(login);
        HttpContext.Session.Clear();
        HttpContext.Session.SetUser(user);
        return user;
    }

    private bool IsReturnUrlFor(string? returnUrl, string role)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl)
            || returnUrl.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var otherRoleArea = role == Roles.Organiser ? "/Participant" : "/Organiser";
        return !returnUrl.StartsWith(otherRoleArea, StringComparison.OrdinalIgnoreCase);
    }

    private static RegisterViewModel ClearPasswords(RegisterViewModel model)
    {
        model.Password = string.Empty;
        model.ConfirmPassword = string.Empty;
        return model;
    }
}
