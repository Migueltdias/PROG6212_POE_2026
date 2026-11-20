using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId => User.GetUserId();

    protected int? CurrentUserIdOrNull => User.TryGetUserId();

    protected ActionResult<T> OkOrError<T>(ServiceResult<T> result) =>
        result.Succeeded ? Ok(result.Value) : ErrorResult(result.Error);

    protected IActionResult NoContentOrError(ServiceResult result) =>
        result.Succeeded ? NoContent() : ErrorResult(result.Error);

    protected ActionResult ErrorResult(ServiceError error)
    {
        if (error.Kind == ServiceErrorKind.Validation)
        {
            ModelState.AddModelError(error.Field ?? string.Empty, error.Message);
            return ValidationProblem(ModelState);
        }

        var status = error.Kind switch
        {
            ServiceErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ServiceErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            ServiceErrorKind.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorKind.Conflict => StatusCodes.Status409Conflict,
            ServiceErrorKind.Unavailable => StatusCodes.Status503ServiceUnavailable,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(detail: error.Message, statusCode: status, title: ReasonPhrases.GetReasonPhrase(status));
    }
}
