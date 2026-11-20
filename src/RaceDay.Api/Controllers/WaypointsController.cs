using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure.Swagger;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>The ordered points that make up a category's route.</summary>
[Tags("Route Waypoints")]
[Route("api")]
[Authorize]
public class WaypointsController(IWaypointService waypoints) : ApiControllerBase
{
    /// <summary>Returns the route in sequence order.</summary>
    /// <param name="id">The category id.</param>
    /// <response code="200">The waypoints, start to finish (empty if no route has been entered).</response>
    /// <response code="404">There is no such category, or its event is a draft the caller does not own.</response>
    [HttpGet("categories/{id:int}/waypoints")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<WaypointDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<WaypointDto>>> List(int id, CancellationToken cancellationToken) =>
        OkOrError(await waypoints.ListAsync(id, CurrentUserIdOrNull, cancellationToken));

    /// <summary>Adds a point to a route.</summary>
    /// <param name="id">The category id.</param>
    /// <param name="request">The new waypoint.</param>
    /// <response code="201">The new waypoint.</response>
    /// <response code="400">Coordinates or type out of range, or further from the start than the route is long.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such category.</response>
    /// <response code="409">That sequence number is already used on this route.</response>
    [HttpPost("categories/{id:int}/waypoints")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(WaypointDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WaypointDto>> Create(int id, WaypointRequest request, CancellationToken cancellationToken)
    {
        var result = await waypoints.CreateAsync(id, CurrentUserId, request, cancellationToken);
        if (!result.Succeeded)
        {
            return ErrorResult(result.Error);
        }

        return Created($"/api/categories/{id}/waypoints", result.Value);
    }

    /// <summary>Updates one waypoint.</summary>
    /// <param name="id">The waypoint id.</param>
    /// <param name="request">The waypoint's new details.</param>
    /// <response code="200">The updated waypoint.</response>
    /// <response code="400">A field is invalid.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such waypoint.</response>
    /// <response code="409">The new sequence number is already used on this route.</response>
    [HttpPut("waypoints/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(WaypointDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WaypointDto>> Update(int id, WaypointRequest request, CancellationToken cancellationToken) =>
        OkOrError(await waypoints.UpdateAsync(id, CurrentUserId, request, cancellationToken));

    /// <summary>Removes a waypoint from a route.</summary>
    /// <param name="id">The waypoint id.</param>
    /// <response code="204">The waypoint was removed.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such waypoint.</response>
    [HttpDelete("waypoints/{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await waypoints.DeleteAsync(id, CurrentUserId, cancellationToken));
}
