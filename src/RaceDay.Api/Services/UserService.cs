using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;
using RaceDay.Api.Security;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Services;

public interface IUserService
{
    Task<ServiceResult<UserProfileDto>> GetProfileAsync(int userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> ChangePasswordAsync(int userId, string? currentSessionId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeactivateAsync(int userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<UserProfileDto>>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserProfileDto>> GetByIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserProfileDto>> SetProfilePictureAsync(int userId, ImageUpload upload, CancellationToken cancellationToken = default);
    Task<ServiceResult> RemoveProfilePictureAsync(int userId, CancellationToken cancellationToken = default);
}

public sealed class UserService(
    RaceDayDbContext db,
    IPasswordHasher<User> passwordHasher,
    ISessionStore sessions,
    TimeProvider clock,
    IImageService images) : IUserService
{
    public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(int userId, CancellationToken cancellationToken = default) =>
        await GetByIdAsync(userId, cancellationToken);

    public async Task<ServiceResult<UserProfileDto>> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return Errors.NotFound("The account no longer exists.");
        }

        if (UserRules.ValidateDateOfBirth(request.DateOfBirth!.Value, user.Role.RoleName, clock.Today()) is { } dobError)
        {
            return dobError;
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = UserRules.NormaliseOptional(request.PhoneNumber);
        user.DateOfBirth = request.DateOfBirth.Value;
        user.Gender = DomainValue.Canonical(request.Gender, Genders.All)!;

        await db.SaveChangesAsync(cancellationToken);
        return user.ToProfileDto().WithReadablePicture(images);
    }

    public async Task<ServiceResult> ChangePasswordAsync(int userId, string? currentSessionId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return Errors.NotFound("The account no longer exists.");
        }

        if (passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            return Errors.Unauthorized("The current password is incorrect.");
        }

        if (request.NewPassword == request.CurrentPassword)
        {
            return Errors.Validation("newPassword", "The new password must be different from the current one.");
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await db.SaveChangesAsync(cancellationToken);

        sessions.RevokeAllForUser(userId, exceptSessionId: currentSessionId);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeactivateAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return Errors.NotFound("The account no longer exists.");
        }

        var today = clock.Today();
        if (user.Role.RoleName == RoleNames.Organiser
            && await db.Events.AnyAsync(e => e.OrganiserId == userId
                                             && e.EventStatus == EventStatuses.Published
                                             && e.EventDate >= today, cancellationToken))
        {
            return Errors.Conflict("You still own a published event that has not taken place. Cancel or complete it first.");
        }

        user.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
        sessions.RevokeAllForUser(userId);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<PagedResult<UserProfileDto>>> ListAsync(UserListQuery query, CancellationToken cancellationToken = default)
    {
        var users = db.Users.AsNoTracking();

        var role = DomainValue.Canonical(query.Role, RoleNames.All);
        if (role is not null)
        {
            users = users.Where(u => u.Role.RoleName == role);
        }

        var page = await users
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ThenBy(u => u.UserId)
            .Select(UserMappings.ToProfile)
            .ToPagedResultAsync(query, cancellationToken);

        foreach (var profile in page.Items)
        {
            profile.WithReadablePicture(images);
        }

        return page;
    }

    public async Task<ServiceResult<UserProfileDto>> GetByIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var profile = await db.Users.AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(UserMappings.ToProfile)
            .SingleOrDefaultAsync(cancellationToken);

        return profile is null ? Errors.NotFound($"There is no user with id {userId}.") : profile.WithReadablePicture(images);
    }

    public async Task<ServiceResult<UserProfileDto>> SetProfilePictureAsync(int userId, ImageUpload upload, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return Errors.NotFound("The account no longer exists.");
        }

        var saved = await images.SaveAsync(ImageKind.ProfilePicture, $"users/{userId}", upload, cancellationToken);
        if (!saved.Succeeded)
        {
            return saved.Error;
        }

        var previous = user.ProfilePictureUrl;
        user.ProfilePictureUrl = saved.Value;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await images.RemoveAsync(saved.Value, CancellationToken.None);
            throw;
        }

        await images.RemoveAsync(previous, cancellationToken);
        return user.ToProfileDto().WithReadablePicture(images);
    }

    public async Task<ServiceResult> RemoveProfilePictureAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return Errors.NotFound("The account no longer exists.");
        }

        var previous = user.ProfilePictureUrl;
        if (previous is null)
        {
            return ServiceResult.Success();
        }

        user.ProfilePictureUrl = null;
        await db.SaveChangesAsync(cancellationToken);
        await images.RemoveAsync(previous, cancellationToken);
        return ServiceResult.Success();
    }
}
