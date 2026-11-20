using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Services;

public interface IAuthService
{
    Task<ServiceResult<UserProfileDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<TokenResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    ServiceResult Logout(string? sessionId);
}

public sealed class AuthService(
    RaceDayDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenService tokens,
    ISessionStore sessions,
    TimeProvider clock,
    IImageService images) : IAuthService
{
    private const string InvalidCredentials = "The email address or password is incorrect.";

    public async Task<ServiceResult<UserProfileDto>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var roleName = DomainValue.Canonical(request.Role, RoleNames.All);
        var role = roleName is null ? null : await db.Roles.SingleOrDefaultAsync(r => r.RoleName == roleName, cancellationToken);
        if (role is null)
        {
            return Errors.Validation("role", "Role must be Organiser or Participant.");
        }

        var dateOfBirth = request.DateOfBirth!.Value;
        if (UserRules.ValidateDateOfBirth(dateOfBirth, role.RoleName, clock.Today()) is { } dobError)
        {
            return dobError;
        }

        var email = UserRules.NormaliseEmail(request.Email);
        if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Errors.Conflict("An account with that email address already exists.");
        }

        var user = new User
        {
            RoleId = role.RoleId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = UserRules.NormaliseOptional(request.PhoneNumber),
            DateOfBirth = dateOfBirth,
            Gender = DomainValue.Canonical(request.Gender, Genders.All)!,
            CreatedAt = clock.UtcNowSeconds(),
            IsActive = true
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        user.Role = role;
        return user.ToProfileDto();
    }

    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = UserRules.NormaliseEmail(request.Email);
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            return Errors.Unauthorized(InvalidCredentials);
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Errors.Unauthorized(InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Errors.Unauthorized("This account has been deactivated.");
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            await db.SaveChangesAsync(cancellationToken);
        }

        var grant = sessions.Create(user.UserId, user.Role.RoleName, user.Email);
        var token = tokens.CreateAccessToken(user, user.Role.RoleName, grant.Session.SessionId);

        return new LoginResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = grant.RefreshToken,
            SessionExpiresAt = grant.Session.ExpiresAt.UtcDateTime,
            User = user.ToProfileDto().WithReadablePicture(images)
        };
    }

    public async Task<ServiceResult<TokenResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var session = sessions.FindActiveByRefreshToken(refreshToken);
        if (session is null)
        {
            return Errors.Unauthorized("The refresh token has expired or been revoked. Log in again.");
        }

        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == session.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            sessions.Revoke(session.SessionId);
            return Errors.Unauthorized("This account has been deactivated.");
        }

        var grant = sessions.Renew(session, user.Role.RoleName);
        var token = tokens.CreateAccessToken(user, user.Role.RoleName, session.SessionId);

        return new TokenResponse
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            RefreshToken = grant.RefreshToken,
            SessionExpiresAt = grant.Session.ExpiresAt.UtcDateTime
        };
    }

    public ServiceResult Logout(string? sessionId)
    {
        if (sessionId is not null)
        {
            sessions.Revoke(sessionId);
        }

        return ServiceResult.Success();
    }
}
