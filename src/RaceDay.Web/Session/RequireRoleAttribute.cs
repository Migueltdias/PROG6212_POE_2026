using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Session;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public class RequireRoleAttribute(params string[] roles) : Attribute, IAuthorizationFilter
{
    public IReadOnlyList<string> Roles { get; } = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var user = http.GetSessionUser();

        if (user is null)
        {
            var returnUrl = HttpMethods.IsGet(http.Request.Method)
                ? http.Request.PathBase + http.Request.Path + http.Request.QueryString
                : null;
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl });
            return;
        }

        if (Roles.Count > 0 && !Roles.Contains(user.Role))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
            return;
        }

        http.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        http.Response.Headers.Pragma = "no-cache";
    }
}

public sealed class RequireLoginAttribute() : RequireRoleAttribute();
