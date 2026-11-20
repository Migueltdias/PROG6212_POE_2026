using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure.Swagger;
using RaceDay.Api.Security;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and session management.</summary>
[Tags("Authentication")]
[Route("api/auth")]
public class AuthController(IAuthService auth) : ApiControllerBase
{
    /// <summary>Creates an Organiser or Participant account.</summary>
    /// <remarks>
    /// The caller picks a role of <c>Organiser</c> or <c>Participant</c>. The role is matched
    /// against the Roles table, cannot be changed later, and Organisers must be at least 18.
    /// The password is stored only as a salted PBKDF2 hash. This does not log the user in:
    /// call POST /api/auth/login next.
    /// </remarks>
    /// <response code="201">The account was created. The body is the new profile with its id.</response>
    /// <response code="400">A field is missing or invalid, or the two passwords differ.</response>
    /// <response code="409">An account with that email address already exists.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserProfileDto>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await auth.RegisterAsync(request, cancellationToken);
        if (!result.Succeeded)
        {
            return ErrorResult(result.Error);
        }

        return Created($"/api/users/{result.Value!.UserId}", result.Value);
    }

    /// <summary>Exchanges credentials for a signed token and starts a session.</summary>
    /// <remarks>
    /// Returns a JWT access token, a single-use refresh token and the user's profile. The
    /// session is kept on the server: it records the user and their role for every later
    /// request until it is logged out, refreshed past its lifetime or ended by a password change.
    /// </remarks>
    /// <response code="200">Logged in. Send <c>token</c> as a Bearer token on later requests.</response>
    /// <response code="400">Email or password was left out.</response>
    /// <response code="401">The credentials are wrong, or the account has been deactivated.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        OkOrError(await auth.LoginAsync(request, cancellationToken));

    /// <summary>Issues a fresh access token.</summary>
    /// <remarks>
    /// Exchanges the refresh token from login (or from a previous refresh) for a new access
    /// token in the same session. Refresh tokens are single use: the one sent here stops
    /// working and a new one is returned. No access token is needed, because this is how a
    /// client recovers once its access token has expired; the refresh token is the credential.
    /// </remarks>
    /// <response code="200">A new access token and refresh token for the same session.</response>
    /// <response code="400">The refresh token was left out.</response>
    /// <response code="401">The refresh token is unknown, already used, expired or revoked.</response>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [AccessNote("None (the refresh token itself is the credential)")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<TokenResponse>> Refresh(RefreshRequest request, CancellationToken cancellationToken) =>
        OkOrError(await auth.RefreshAsync(request.RefreshToken, cancellationToken));

    /// <summary>Ends the caller's session.</summary>
    /// <remarks>
    /// Revokes the session on the server, so the access token sent with this request and its
    /// refresh token are rejected from now on, even though the token has not expired yet.
    /// </remarks>
    /// <response code="204">Logged out.</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout() => NoContentOrError(auth.Logout(User.GetSessionId()));
}
