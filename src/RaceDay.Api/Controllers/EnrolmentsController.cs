using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure.Swagger;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Entries. A Participant enters an event by selecting one of its categories; the entry records
/// the Participant, the category and (through it) the event.
/// </summary>
[Tags("Enrolments")]
[Route("api")]
[Authorize]
public class EnrolmentsController(IEnrolmentService enrolments) : ApiControllerBase
{
    /// <summary>Enters the caller into a category.</summary>
    /// <remarks>
    /// The caller is entered into the selected category of a published event whose entries are
    /// open. The server allocates the race number. The entry is Pending until the fee is paid
    /// (PATCH /api/enrolments/{id}/payment), or Confirmed at once for a free category.
    /// Someone who withdrew from the category can enter again and keeps their race number.
    /// </remarks>
    /// <param name="categoryId">The category being entered.</param>
    /// <param name="request">T-shirt size and emergency contact.</param>
    /// <response code="201">The entry, with its allocated race number.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="403">The caller is an Organiser; only Participants can enter.</response>
    /// <response code="404">There is no such category.</response>
    /// <response code="409">Already entered, category full, entries not open, or under the minimum age.</response>
    [HttpPost("categories/{categoryId:int}/enrolments")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Enter(int categoryId, EnrolmentRequest request, CancellationToken cancellationToken)
    {
        var result = await enrolments.EnterAsync(categoryId, CurrentUserId, request, cancellationToken);
        if (!result.Succeeded)
        {
            return ErrorResult(result.Error);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Value!.EnrolmentId }, result.Value);
    }

    /// <summary>Lists the caller's own entries.</summary>
    /// <remarks>Every entry the caller has made, with event and category details, newest event first.</remarks>
    /// <response code="200">A page of the caller's entries.</response>
    /// <response code="400">A filter or paging value is invalid.</response>
    [HttpGet("enrolments/mine")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(PagedResult<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<EnrolmentDto>>> Mine([FromQuery] MyEnrolmentsQuery query, CancellationToken cancellationToken) =>
        OkOrError(await enrolments.ListMineAsync(CurrentUserId, query, cancellationToken));

    /// <summary>Returns one entry.</summary>
    /// <remarks>Visible to the Participant who made it and to the Organiser running the event; nobody else.</remarks>
    /// <param name="id">The enrolment id.</param>
    /// <response code="200">The entry.</response>
    /// <response code="403">The entry belongs to someone else.</response>
    /// <response code="404">There is no such entry.</response>
    [HttpGet("enrolments/{id:int}")]
    [AccessNote("Participant (own) or Organiser (owner)")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrolmentDto>> Get(int id, CancellationToken cancellationToken) =>
        OkOrError(await enrolments.GetAsync(id, CurrentUserId, User.GetRole(), cancellationToken));

    /// <summary>Updates the caller's own entry details.</summary>
    /// <remarks>Changes the T-shirt size and emergency contact. Not possible once withdrawn or once the event has been run.</remarks>
    /// <param name="id">The enrolment id.</param>
    /// <param name="request">The new details.</param>
    /// <response code="200">The updated entry.</response>
    /// <response code="400">A field is missing or invalid.</response>
    /// <response code="403">The entry belongs to someone else.</response>
    /// <response code="404">There is no such entry.</response>
    /// <response code="409">The entry was withdrawn, or the event has been run.</response>
    [HttpPut("enrolments/{id:int}")]
    [Authorize(Roles = RoleNames.Participant)]
    [AccessNote("Participant (own)")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Update(int id, EnrolmentRequest request, CancellationToken cancellationToken) =>
        OkOrError(await enrolments.UpdateAsync(id, CurrentUserId, request, cancellationToken));

    /// <summary>Records payment and confirms the entry.</summary>
    /// <remarks>The amount must equal the category's entry fee. A Pending entry becomes Confirmed.</remarks>
    /// <param name="id">The enrolment id.</param>
    /// <param name="request">The amount and payment reference.</param>
    /// <response code="200">The confirmed entry.</response>
    /// <response code="400">The amount does not match the entry fee.</response>
    /// <response code="403">The entry belongs to someone else.</response>
    /// <response code="404">There is no such entry.</response>
    /// <response code="409">Already confirmed, withdrawn, or the event has been run.</response>
    [HttpPatch("enrolments/{id:int}/payment")]
    [Authorize(Roles = RoleNames.Participant)]
    [AccessNote("Participant (own)")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentDto>> Pay(int id, PaymentRequest request, CancellationToken cancellationToken) =>
        OkOrError(await enrolments.RecordPaymentAsync(id, CurrentUserId, request, cancellationToken));

    /// <summary>Withdraws the caller from a category.</summary>
    /// <remarks>
    /// The entry is marked Cancelled rather than deleted, so the Organiser can still reconcile
    /// fees against who withdrew and when. Entering the category again reinstates it.
    /// </remarks>
    /// <param name="id">The enrolment id.</param>
    /// <response code="204">Withdrawn.</response>
    /// <response code="403">The entry belongs to someone else.</response>
    /// <response code="404">There is no such entry.</response>
    /// <response code="409">Already withdrawn, or the event has been run.</response>
    [HttpDelete("enrolments/{id:int}")]
    [Authorize(Roles = RoleNames.Participant)]
    [AccessNote("Participant (own)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(int id, CancellationToken cancellationToken) =>
        NoContentOrError(await enrolments.WithdrawAsync(id, CurrentUserId, cancellationToken));

    /// <summary>Returns the start list for one category.</summary>
    /// <remarks>Every entry in the category in race-number order, including emergency contacts, so only the event's Organiser may read it.</remarks>
    /// <param name="id">The category id.</param>
    /// <response code="200">A page of the start list.</response>
    /// <response code="403">The caller did not create the event.</response>
    /// <response code="404">There is no such category.</response>
    [HttpGet("categories/{id:int}/enrolments")]
    [Authorize(Roles = RoleNames.Organiser)]
    [AccessNote("Organiser (owner)")]
    [ProducesResponseType(typeof(PagedResult<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<EnrolmentDto>>> CategoryStartList(int id, [FromQuery] StartListQuery query, CancellationToken cancellationToken) =>
        OkOrError(await enrolments.CategoryStartListAsync(id, CurrentUserId, query, cancellationToken));
}
