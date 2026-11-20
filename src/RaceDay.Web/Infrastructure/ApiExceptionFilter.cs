using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.ApiClient;
using RaceDay.Web.Models;
using RaceDay.Web.Session;

namespace RaceDay.Web.Infrastructure;

public sealed class ApiExceptionFilter(ITempDataDictionaryFactory tempDataFactory, IModelMetadataProvider metadata,
    ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ApiSessionExpiredException expired:
                context.HttpContext.TryGetSession()?.Clear();
                var tempData = tempDataFactory.GetTempData(context.HttpContext);
                tempData[Flash.Warning] = expired.Message;
                var request = context.HttpContext.Request;
                var returnUrl = HttpMethods.IsGet(request.Method) ? request.PathBase + request.Path + request.QueryString : null;
                context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl });
                break;

            case ApiRequestFailedException failed:
                context.Result = failed.Result.StatusCode switch
                {
                    HttpStatusCode.NotFound => Problem(context, ProblemViewModel.NotFound()),
                    HttpStatusCode.Forbidden => Problem(context, ProblemViewModel.Forbidden(failed.Result.Error)),
                    HttpStatusCode.ServiceUnavailable => Problem(context, ProblemViewModel.Unavailable(failed.Result.Error)),
                    _ when (int)failed.Result.StatusCode >= 500 => Problem(context, ProblemViewModel.Unavailable()),
                    _ => Problem(context, ProblemViewModel.ForStatus((int)failed.Result.StatusCode) with { Message = failed.Result.ErrorMessage })
                };
                break;

            case ApiUnavailableException unavailable:
                logger.LogWarning("Showing the service-unavailable page for {Path}: {Message}", context.HttpContext.Request.Path, unavailable.Message);
                context.Result = Problem(context, ProblemViewModel.Unavailable());
                break;

            default:
                return;
        }

        context.ExceptionHandled = true;
    }

    private ViewResult Problem(ExceptionContext context, ProblemViewModel problem) => new()
    {
        ViewName = "Problem",
        StatusCode = problem.StatusCode,
        ViewData = new ViewDataDictionary(metadata, context.ModelState)
        {
            Model = problem with { RequestId = context.HttpContext.TraceIdentifier }
        }
    };
}
