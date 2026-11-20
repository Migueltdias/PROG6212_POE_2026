using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure.Swagger;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Finish times and positions. Organisers capture them after an event; results are public,
/// and Participants can follow their own history.
/// </summary>
[Tags("Results")]
[Route("api")]
[Authorize]
public class ResultsController(IResultService results) : ApiControllerBase
{
    /// <summary>Captures the outcome of one entry.</summary>
    /// <remarks>
    /// Records the finish time and finishing positions for a Participant after the event. Only
    /// confirmed entries can be given a result, and only once the event date has arrived. A
    /// finisher needs a finish time and an overall position; the chip time is worked out from
    /// the start and finish times when it is left out. DNF, DNS and DQ carry no time or position.
    /// </remarks>
    /// <param name="id">The enrolment id.</param>
    /// <param name="request">The outcome.</param>
    /// <response code="201">The new result.</response>
    /// <response code="400">A finish time was sent for a DNF, or left out for a finisher.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such entry.</response>
    /// <response code="409">A result already exists, the entry is not confirmed, or the event has not been run.</response>
    [HttpPost("enrolments/{id:int}/result")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultDto>> Capture(int id, ResultRequest request, CancellationToken cancellationToken)
    {
        var result = await results.CaptureAsync(id, CurrentUserId, request, cancellationToken);
        if (!result.Succeeded)
        {
            return ErrorResult(result.Error);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.ResultId }, result.Value);
    }

    /// <summary>Corrects a captured result.</summary>
    /// <param name="id">The result id.</param>
    /// <param name="request">The corrected outcome.</param>
    /// <response code="200">The corrected result.</response>
    /// <response code="400">The status, time and positions do not agree.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such result.</response>
    [HttpPut("results/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Update(int id, ResultRequest request, CancellationToken cancellationToken) =>
        OkOrError(await results.UpdateAsync(id, CurrentUserId, request, cancellationToken));

    /// <summary>Removes a result so it can be recaptured.</summary>
    /// <param name="id">The result id.</param>
    /// <response code="204">The result was removed.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such result.</response>
    [HttpDelete("results/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await results.DeleteAsync(id, CurrentUserId, cancellationToken));

    /// <summary>Returns one result with its context.</summary>
    /// <remarks>Results are published, so no token is needed.</remarks>
    /// <param name="id">The result id.</param>
    /// <response code="200">The result with its event, category and participant.</response>
    /// <response code="404">There is no such result.</response>
    [HttpGet("results/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultDto>> Get(int id, CancellationToken cancellationToken) =>
        OkOrError(await results.GetAsync(id, cancellationToken));

    /// <summary>Imports a category from a timing file.</summary>
    /// <remarks>
    /// Each row is matched to an entry by race number. Entries that already have a result are
    /// skipped, and rows that cannot be used are reported with a reason rather than failing the
    /// whole file. After the import, overall, gender and age-group positions for the category are
    /// recalculated from chip times (age groups are gender plus ten-year bands, e.g. women 30-39).
    /// </remarks>
    /// <param name="id">The category id.</param>
    /// <param name="request">The rows of the timing file.</param>
    /// <response code="202">{ imported, skipped, errors } describing what happened to each row.</response>
    /// <response code="400">The payload is malformed, e.g. no rows or an unknown status.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such category.</response>
    /// <response code="409">The event has not been run yet.</response>
    [HttpPost("categories/{id:int}/results/bulk")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(BulkImportResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BulkImportResponse>> Import(int id, BulkResultsRequest request, CancellationToken cancellationToken)
    {
        var result = await results.ImportAsync(id, CurrentUserId, request, cancellationToken);
        return result.Succeeded ? Accepted(result.Value) : ErrorResult(result.Error);
    }

    /// <summary>Returns the caller's performance history.</summary>
    /// <remarks>Every result the caller has, newest event first, with times and placings.</remarks>
    /// <response code="200">A page of the caller's results.</response>
    /// <response code="400">A filter or paging value is invalid.</response>
    [HttpGet("results/mine")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(PagedResult<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ResultDto>>> Mine([FromQuery] MyResultsQuery query, CancellationToken cancellationToken) =>
        OkOrError(await results.ListMineAsync(CurrentUserId, query, cancellationToken));

    /// <summary>Returns the leaderboard for a category.</summary>
    /// <remarks>Finishers in position order, then DNF, DNS and DQ. Filter by gender to rank by gender position.</remarks>
    /// <param name="id">The category id.</param>
    /// <response code="200">A page of the leaderboard.</response>
    /// <response code="400">The gender or paging values are invalid.</response>
    /// <response code="404">There is no such category.</response>
    [HttpGet("categories/{id:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<LeaderboardEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<LeaderboardEntryDto>>> Leaderboard(int id, [FromQuery] LeaderboardQuery query, CancellationToken cancellationToken) =>
        OkOrError(await results.LeaderboardAsync(id, query, cancellationToken));
}
