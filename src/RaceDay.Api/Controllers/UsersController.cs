using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>The signed-in user's own profile, and account lookups for Organisers.</summary>
[Tags("User Profile")]
[Route("api/users")]
[Authorize]
public class UsersController(IUserService users) : ApiControllerBase
{
    /// <summary>Returns the caller's own profile.</summary>
    /// <remarks>The user is identified from the token, never from the URL.</remarks>
    /// <response code="200">The caller's profile.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> GetMe(CancellationToken cancellationToken) =>
        OkOrError(await users.GetProfileAsync(CurrentUserId, cancellationToken));

    /// <summary>Updates the caller's editable details.</summary>
    /// <remarks>Name, phone number, date of birth and gender can change. Email and role cannot.</remarks>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">A field is missing or invalid.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileRequest request, CancellationToken cancellationToken) =>
        OkOrError(await users.UpdateProfileAsync(CurrentUserId, request, cancellationToken));

    /// <summary>Changes the caller's password.</summary>
    /// <remarks>
    /// The current password must be correct. Every other session for the account is signed out;
    /// the session making the change stays signed in.
    /// </remarks>
    /// <response code="204">The password was changed.</response>
    /// <response code="400">The new passwords differ, fail the password policy, or match the old one.</response>
    /// <response code="401">The current password is wrong.</response>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken) =>
        NoContentOrError(await users.ChangePasswordAsync(CurrentUserId, User.GetSessionId(), request, cancellationToken));

    /// <summary>Deactivates the caller's account.</summary>
    /// <remarks>
    /// The account is switched off rather than deleted, so past entries and results stay intact.
    /// All of its sessions end immediately and it can no longer log in.
    /// </remarks>
    /// <response code="204">The account was deactivated.</response>
    /// <response code="409">The caller is an Organiser who still owns a published event that has not taken place.</response>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeactivateMe(CancellationToken cancellationToken) =>
        NoContentOrError(await users.DeactivateAsync(CurrentUserId, cancellationToken));

    /// <summary>Uploads or replaces the caller's profile picture.</summary>
    /// <remarks>
    /// Send the image as multipart/form-data in a field called <c>file</c>: JPEG, PNG, GIF or WebP,
    /// up to 5 MB. The image is stored in Azure Blob Storage, its URL is saved on the account,
    /// and any previous picture is deleted.
    /// </remarks>
    /// <param name="request">The image file.</param>
    /// <response code="200">The profile with its new picture.</response>
    /// <response code="400">No file was sent, it is too large, or it is not a supported image.</response>
    /// <response code="503">Azure Blob Storage cannot be reached, so nothing was changed.</response>
    [HttpPut("me/profile-picture")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ImageUploadRequest.MaxRequestBytes)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<UserProfileDto>> UploadProfilePicture([FromForm] ImageUploadRequest request, CancellationToken cancellationToken) =>
        OkOrError(await users.SetProfilePictureAsync(CurrentUserId, request.ToUpload(), cancellationToken));

    /// <summary>Removes the caller's profile picture.</summary>
    /// <remarks>Deletes the image from Azure Blob Storage and clears its URL. Succeeds if there was no picture.</remarks>
    /// <response code="204">The picture was removed.</response>
    [HttpDelete("me/profile-picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveProfilePicture(CancellationToken cancellationToken) =>
        NoContentOrError(await users.RemoveProfilePictureAsync(CurrentUserId, cancellationToken));

    /// <summary>Lists accounts, filtered by role.</summary>
    /// <remarks>Sorted by surname. Use <c>role</c> to list only Organisers or only Participants.</remarks>
    /// <response code="200">A page of profiles.</response>
    /// <response code="400">The role or paging values are invalid.</response>
    [HttpGet]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(PagedResult<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<UserProfileDto>>> List([FromQuery] UserListQuery query, CancellationToken cancellationToken) =>
        OkOrError(await users.ListAsync(query, cancellationToken));

    /// <summary>Returns one profile by id.</summary>
    /// <param name="id">The user id.</param>
    /// <response code="200">The profile.</response>
    /// <response code="404">There is no user with that id.</response>
    [HttpGet("{id:int}")]
    [Authorize(Roles = RoleNames.Organiser)]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetById(int id, CancellationToken cancellationToken) =>
        OkOrError(await users.GetByIdAsync(id, cancellationToken));
}
