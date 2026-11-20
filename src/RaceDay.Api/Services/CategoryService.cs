using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public interface ICategoryService
{
    Task<ServiceResult<List<CategoryDto>>> ListForEventAsync(int eventId, int? callerId, CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryDto>> GetAsync(int categoryId, int? callerId, CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryDto>> CreateAsync(int eventId, int organiserId, CategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<CategoryDto>> UpdateAsync(int categoryId, int organiserId, CategoryRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int categoryId, int organiserId, CancellationToken cancellationToken = default);
}

public sealed class CategoryService(RaceDayDbContext db) : ICategoryService
{
    public async Task<ServiceResult<List<CategoryDto>>> ListForEventAsync(int eventId, int? callerId, CancellationToken cancellationToken = default)
    {
        if (!await db.Events.Where(e => e.EventId == eventId).VisibleTo(callerId).AnyAsync(cancellationToken))
        {
            return Errors.NotFound($"There is no event with id {eventId}.");
        }

        return await db.EventCategories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderByDescending(c => c.DistanceKm).ThenBy(c => c.CategoryName)
            .Select(CategoryMappings.ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<CategoryDto>> GetAsync(int categoryId, int? callerId, CancellationToken cancellationToken = default)
    {
        var category = await db.EventCategories.AsNoTracking()
            .Where(c => c.CategoryId == categoryId
                        && (c.Event.EventStatus != EventStatuses.Draft || c.Event.OrganiserId == callerId))
            .Select(CategoryMappings.ToDto)
            .SingleOrDefaultAsync(cancellationToken);

        return category is null ? Errors.NotFound($"There is no category with id {categoryId}.") : category;
    }

    public async Task<ServiceResult<CategoryDto>> CreateAsync(int eventId, int organiserId, CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var ev = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (ev is null)
        {
            return Errors.NotFound($"There is no event with id {eventId}.");
        }

        if ((Ownership.RequireOwner(ev, organiserId) ?? EnsureEditable(ev)) is { } refused)
        {
            return refused;
        }

        if (await NameTakenAsync(eventId, request.CategoryName, exceptCategoryId: null, cancellationToken))
        {
            return Errors.Conflict($"This event already has a category called '{request.CategoryName.Trim()}'.");
        }

        var category = request.ToEntity(eventId);
        db.EventCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(category.CategoryId, organiserId, cancellationToken);
    }

    public async Task<ServiceResult<CategoryDto>> UpdateAsync(int categoryId, int organiserId, CategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await db.EventCategories.Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Errors.NotFound($"There is no category with id {categoryId}.");
        }

        if ((Ownership.RequireOwner(category.Event, organiserId) ?? EnsureEditable(category.Event)) is { } refused)
        {
            return refused;
        }

        if (await NameTakenAsync(category.EventId, request.CategoryName, exceptCategoryId: categoryId, cancellationToken))
        {
            return Errors.Conflict($"This event already has a category called '{request.CategoryName.Trim()}'.");
        }

        var entered = await db.Enrolments.CountAsync(en => en.CategoryId == categoryId
                                                           && EnrolmentStatuses.Active.Contains(en.EnrolmentStatus), cancellationToken);
        if (request.MaxParticipants < entered)
        {
            return Errors.Conflict($"{entered} people have already entered, so the limit cannot be lowered to {request.MaxParticipants}.");
        }

        var updated = request.ToEntity(category.EventId);
        category.CategoryName = updated.CategoryName;
        category.Discipline = updated.Discipline;
        category.DistanceKm = updated.DistanceKm;
        category.EntryFee = updated.EntryFee;
        category.MaxParticipants = updated.MaxParticipants;
        category.StartTime = updated.StartTime;
        category.MinimumAge = updated.MinimumAge;
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(categoryId, organiserId, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(int categoryId, int organiserId, CancellationToken cancellationToken = default)
    {
        var category = await db.EventCategories
            .Include(c => c.Event).ThenInclude(e => e.Categories)
            .Include(c => c.Waypoints)
            .Include(c => c.Enrolments)
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Errors.NotFound($"There is no category with id {categoryId}.");
        }

        if (Ownership.RequireOwner(category.Event, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        if (category.Enrolments.Count > 0)
        {
            return Errors.Conflict("People have entered this category, so it cannot be removed.");
        }

        if (category.Event.EventStatus == EventStatuses.Published && category.Event.Categories.Count == 1)
        {
            return Errors.Conflict("A published event must keep at least one category.");
        }

        db.EventCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static ServiceError? EnsureEditable(Event ev) =>
        ev.EventStatus is EventStatuses.Cancelled or EventStatuses.Completed
            ? Errors.Conflict($"This event is {ev.EventStatus}, so its categories can no longer be changed.")
            : null;

    private Task<bool> NameTakenAsync(int eventId, string categoryName, int? exceptCategoryId, CancellationToken cancellationToken)
    {
        var name = categoryName.Trim().ToLower();
        return db.EventCategories.AnyAsync(c => c.EventId == eventId
                                                && c.CategoryName.ToLower() == name
                                                && c.CategoryId != exceptCategoryId, cancellationToken);
    }
}
