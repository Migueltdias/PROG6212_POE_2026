using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure.Swagger;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Events. Both roles (and anonymous visitors) can browse them; Organisers create and manage
/// their own. An event's distances and types (Run, Walk, Cycle) are its categories.
/// </summary>
[Tags("Events")]
[Route("api/events")]
[Authorize]
public class EventsController(
    IEventService events,
    IEnrolmentService enrolments,
    IResultService results,
    IWeatherService weather) : ApiControllerBase
{
    /// <summary>Lists published events for browsing.</summary>
    /// <remarks>
    /// Returns every event that has been published (including completed and cancelled ones),
    /// soonest first. Drafts never appear here. Filter by province, event type, date range
    /// or a search term; each summary lists its disciplines and the distances on offer.
    /// </remarks>
    /// <response code="200">A page of event summaries.</response>
    /// <response code="400">A filter or paging value is invalid.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<EventSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EventSummaryDto>>> Browse([FromQuery] EventQuery query, CancellationToken cancellationToken) =>
        OkOrError(await events.BrowseAsync(query, cancellationToken));

    /// <summary>Returns one event with its categories.</summary>
    /// <remarks>A draft is only visible to the Organiser who created it; to everyone else it is a 404.</remarks>
    /// <param name="id">The event id.</param>
    /// <response code="200">The event, with each category and the places left in it.</response>
    /// <response code="404">There is no such event, or it is a draft the caller does not own.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailDto>> Get(int id, CancellationToken cancellationToken) =>
        OkOrError(await events.GetAsync(id, CurrentUserIdOrNull, cancellationToken));

    /// <summary>Creates a draft event.</summary>
    /// <remarks>
    /// The caller becomes the event's Organiser. The event starts as a Draft that only they can
    /// see; publish it with PATCH /api/events/{id}/status once it has at least one category.
    /// Categories (each with its distance and event type) can be included here or added later.
    /// </remarks>
    /// <response code="201">The new event, with its id and any categories created with it.</response>
    /// <response code="400">A field is missing or invalid, the date is in the past, or the registration window does not fit before the event.</response>
    /// <response code="409">Another event already uses that name on that date.</response>
    [HttpPost]
    [Authorize(Roles = Domain.RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetailDto>> Create(CreateEventRequest request, CancellationToken cancellationToken)
    {
        var result = await events.CreateAsync(CurrentUserId, request, cancellationToken);
        if (!result.Succeeded)
        {
            return ErrorResult(result.Error);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.EventId }, result.Value);
    }

    /// <summary>Updates an event the caller owns.</summary>
    /// <remarks>Replaces the event's details. Cancelled and completed events can no longer be edited.</remarks>
    /// <param name="id">The event id.</param>
    /// <param name="request">The event's new details.</param>
    /// <response code="200">The updated event.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="403">The caller did not create this event.</response>
    /// <response code="404">There is no such event.</response>
    /// <response code="409">The new name is already used on that date, or the event is cancelled or completed.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Domain.RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetailDto>> Update(int id, EventRequest request, CancellationToken cancellationToken) =>
        OkOrError(await events.UpdateAsync(id, CurrentUserId, request, cancellationToken));

    /// <summary>Publishes, cancels or completes an event.</summary>
    /// <remarks>
    /// Allowed changes: Draft to Published or Cancelled; Published to Cancelled or Completed.
    /// Cancelled and Completed are final. An event needs at least one category to be published,
    /// and cannot be completed before its date.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <param name="request">The new status.</param>
    /// <response code="200">The event with its new status.</response>
    /// <response code="400">The status is not a known value.</response>
    /// <response code="403">The caller did not create this event.</response>
    /// <response code="404">There is no such event.</response>
    /// <response code="409">The change is not allowed, e.g. publishing an event with no categories.</response>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = Domain.RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EventDetailDto>> ChangeStatus(int id, EventStatusRequest request, CancellationToken cancellationToken) =>
        OkOrError(await events.ChangeStatusAsync(id, CurrentUserId, request.Status, cancellationToken));

    /// <summary>Deletes an event and its categories.</summary>
    /// <remarks>
    /// Removes the event with its categories, routes, unconfirmed entries and weather. An event
    /// with confirmed entries cannot be deleted; cancel it instead so the entries are kept.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <response code="204">The event was deleted.</response>
    /// <response code="403">The caller did not create this event.</response>
    /// <response code="404">There is no such event.</response>
    /// <response code="409">The event has confirmed entries.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Domain.RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await events.DeleteAsync(id, CurrentUserId, cancellationToken));

    /// <summary>Uploads or replaces the event's banner image.</summary>
    /// <remarks>
    /// Send the image as multipart/form-data in a field called <c>file</c>: JPEG, PNG, GIF or WebP,
    /// up to 5 MB. The file type is read from its contents, not its name. The image is stored in
    /// Azure Blob Storage, its URL is saved on the event, and any previous banner is deleted.
    /// The returned event carries a read-only link to the new banner.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <param name="request">The image file.</param>
    /// <response code="200">The event with its new banner.</response>
    /// <response code="400">No file was sent, it is too large, or it is not a supported image.</response>
    /// <response code="403">The caller did not create this event.</response>
    /// <response code="404">There is no such event.</response>
    /// <response code="409">The event is cancelled or completed.</response>
    /// <response code="503">Azure Blob Storage cannot be reached, so nothing was changed.</response>
    [HttpPut("{id:int}/banner")]
    [Authorize(Roles = Domain.RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageUploadRequest.MaxRequestBytes)]
    [ProducesResponseType(typeof(EventDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<EventDetailDto>> UploadBanner(int id, [FromForm] ImageUploadRequest request, CancellationToken cancellationToken) =>
        OkOrError(await events.SetBannerAsync(id, CurrentUserId, request.ToUpload(), cancellationToken));

    /// <summary>Removes the event's banner image.</summary>
    /// <remarks>Deletes the image from Azure Blob Storage and clears its URL. Succeeds if there was no banner.</remarks>
    /// <param name="id">The event id.</param>
    /// <response code="204">The banner was removed.</response>
    /// <response code="403">The caller did not create this event.</response>
    /// <response code="404">There is no such event.</response>
    /// <response code="409">The event is cancelled or completed.</response>
    [HttpDelete("{id:int}/banner")]
    [Authorize(Roles = Domain.RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveBanner(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await events.RemoveBannerAsync(id, CurrentUserId, cancellationToken));

    /// <summary>Lists the caller's events, drafts included.</summary>
    /// <remarks>Every event the calling Organiser created, newest event date first, optionally filtered by status.</remarks>
    /// <response code="200">A page of the caller's events.</response>
    /// <response code="400">The status or paging values are invalid.</response>
    [HttpGet("mine")]
    [Authorize(Roles = Domain.RoleNames.Organiser)]
    [ProducesResponseType(typeof(PagedResult<EventSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EventSummaryDto>>> Mine([FromQuery] MyEventsQuery query, CancellationToken cancellationToken) =>
        OkOrError(await events.ListMineAsync(CurrentUserId, query, cancellationToken));

    /// <summary>Returns the start list for a whole event.</summary>
    /// <remarks>
    /// Every entry across the event's categories (longest distance first, then race number),
    /// including emergency contacts. Filter by status or by one category of the event.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <response code="200">A page of the event's entries.</response>
    /// <response code="400">The category is not part of this event, or a filter is invalid.</response>
    /// <response code="403">The caller did not create this event.</response>
    /// <response code="404">There is no such event.</response>
    [HttpGet("{id:int}/enrolments")]
    [Authorize(Roles = Domain.RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(PagedResult<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<EnrolmentDto>>> StartList(int id, [FromQuery] EventStartListQuery query, CancellationToken cancellationToken) =>
        OkOrError(await enrolments.EventStartListAsync(id, CurrentUserId, query, cancellationToken));

    /// <summary>Returns event results by category.</summary>
    /// <remarks>
    /// One leaderboard per category, longest distance first; finishers in position order, then
    /// DNF, DNS and DQ. Results are published, so no token is needed.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <response code="200">The event's results grouped by category.</response>
    /// <response code="404">There is no such event, or it is a draft.</response>
    [HttpGet("{id:int}/results")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventResultsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResultsDto>> Results(int id, CancellationToken cancellationToken) =>
        OkOrError(await results.EventResultsAsync(id, cancellationToken));

    /// <summary>Returns cached weather for the venue.</summary>
    /// <remarks>
    /// The latest reading from the weather cache for the event's coordinates. In this part the
    /// cache is read only; fetching live readings from the provider is added in Part 3.
    /// </remarks>
    /// <param name="id">The event id.</param>
    /// <response code="200">The most recent weather snapshot.</response>
    /// <response code="404">There is no such event, or it is a draft the caller does not own.</response>
    /// <response code="503">No weather has been cached for the event and the provider is unavailable.</response>
    [HttpGet("{id:int}/weather")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WeatherSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<WeatherSnapshotDto>> Weather(int id, CancellationToken cancellationToken) =>
        OkOrError(await weather.GetLatestAsync(id, CurrentUserIdOrNull, cancellationToken));
}
