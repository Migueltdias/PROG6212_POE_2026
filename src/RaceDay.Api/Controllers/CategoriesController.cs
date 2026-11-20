using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure.Swagger;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// The distance or age categories offered at an event (e.g. "10km", "21.1km", "Under 20").
/// Created under their event, then edited by their own id.
/// </summary>
[Tags("Event Categories")]
[Route("api")]
[Authorize]
public class CategoriesController(ICategoryService categories) : ApiControllerBase
{
    /// <summary>Lists the distances an event offers.</summary>
    /// <remarks>Longest distance first, each with its fee, start time, minimum age and places left.</remarks>
    /// <param name="eventId">The event id.</param>
    /// <response code="200">The event's categories.</response>
    /// <response code="404">There is no such event, or it is a draft the caller does not own.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryDto>>> ListForEvent(int eventId, CancellationToken cancellationToken) =>
        OkOrError(await categories.ListForEventAsync(eventId, CurrentUserIdOrNull, cancellationToken));

    /// <summary>Returns one category and its places left.</summary>
    /// <param name="id">The category id.</param>
    /// <response code="200">The category.</response>
    /// <response code="404">There is no such category, or its event is a draft the caller does not own.</response>
    [HttpGet("categories/{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> Get(int id, CancellationToken cancellationToken) =>
        OkOrError(await categories.GetAsync(id, CurrentUserIdOrNull, cancellationToken));

    /// <summary>Adds a distance to an event.</summary>
    /// <remarks>
    /// Defines a distance or age category with its event type (Run, Walk or Cycle), fee, field
    /// limit, start time and minimum age. Names must be unique within the event.
    /// </remarks>
    /// <param name="eventId">The event id.</param>
    /// <param name="request">The new category.</param>
    /// <response code="201">The new category.</response>
    /// <response code="400">A field is missing or out of range.</response>
    /// <response code="403">The caller did not create this event.</response>
    /// <response code="404">There is no such event.</response>
    /// <response code="409">The event already has a category with that name, or it is cancelled or completed.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(int eventId, CategoryRequest request, CancellationToken cancellationToken)
    {
        var result = await categories.CreateAsync(eventId, CurrentUserId, request, cancellationToken);
        if (!result.Succeeded)
        {
            return ErrorResult(result.Error);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.CategoryId }, result.Value);
    }

    /// <summary>Updates a category.</summary>
    /// <param name="id">The category id.</param>
    /// <param name="request">The category's new details.</param>
    /// <response code="200">The updated category.</response>
    /// <response code="400">A field is missing or out of range.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such category.</response>
    /// <response code="409">The new limit is below the number already entered, or the name is taken.</response>
    [HttpPut("categories/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request, CancellationToken cancellationToken) =>
        OkOrError(await categories.UpdateAsync(id, CurrentUserId, request, cancellationToken));

    /// <summary>Removes a category.</summary>
    /// <remarks>Also removes its route. Refused once anyone has entered it.</remarks>
    /// <param name="id">The category id.</param>
    /// <response code="204">The category was removed.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such category.</response>
    /// <response code="409">Entries exist, or it is the last category of a published event.</response>
    [HttpDelete("categories/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await categories.DeleteAsync(id, CurrentUserId, cancellationToken));
}
