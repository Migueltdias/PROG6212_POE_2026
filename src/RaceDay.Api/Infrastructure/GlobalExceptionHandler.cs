using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Infrastructure;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict",
                "The record was changed by another request. Reload it and try again."),
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflict",
                "The change conflicts with data that is already stored. Reload and try again."),
            BadHttpRequestException bad => (bad.StatusCode, "Bad Request", bad.Message),
            _ when IsDatabaseUnavailable(exception) => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable",
                "The database cannot be reached. Try again shortly."),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error",
                "An unexpected error occurred.")
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request refused with {Status} for {Method} {Path}", status, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }

    private static bool IsDatabaseUnavailable(Exception exception) =>
        exception is SqlException or TimeoutException
        || exception.InnerException is SqlException
        || exception is InvalidOperationException { InnerException: SqlException };
}
