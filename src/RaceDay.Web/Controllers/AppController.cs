using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Infrastructure;
using RaceDay.Web.Session;

namespace RaceDay.Web.Controllers;

public abstract class AppController : Controller
{
    protected SessionUser? SignedInUser => HttpContext.GetSessionUser();

    protected SessionUser CurrentUser =>
        SignedInUser ?? throw new InvalidOperationException("This page needs a signed-in user; add [RequireLogin] or [RequireRole].");

    protected void FlashSuccess(string message) => TempData[Flash.Success] = message;

    protected void FlashError(string message) => TempData[Flash.Error] = message;

    protected void FlashWarning(string message) => TempData[Flash.Warning] = message;

    protected void FlashInfo(string message) => TempData[Flash.Info] = message;

    protected IActionResult RedirectToRoleHome(string role)
    {
        var (action, controller, area) = RoleHome.For(role);
        return RedirectToAction(action, controller, new { area });
    }
}
