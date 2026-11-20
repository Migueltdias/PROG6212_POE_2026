using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public interface IWaypointService
{
    Task<ServiceResult<List<WaypointDto>>> ListAsync(int categoryId, int? callerId, CancellationToken cancellationToken = default);
    Task<ServiceResult<WaypointDto>> CreateAsync(int categoryId, int organiserId, WaypointRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<WaypointDto>> UpdateAsync(int waypointId, int organiserId, WaypointRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int waypointId, int organiserId, CancellationToken cancellationToken = default);
}

public sealed class WaypointService(RaceDayDbContext db) : IWaypointService
{
    public async Task<ServiceResult<List<WaypointDto>>> ListAsync(int categoryId, int? callerId, CancellationToken cancellationToken = default)
    {
        var visible = await db.EventCategories.AnyAsync(c => c.CategoryId == categoryId
            && (c.Event.EventStatus != EventStatuses.Draft || c.Event.OrganiserId == callerId), cancellationToken);
        if (!visible)
        {
            return Errors.NotFound($"There is no category with id {categoryId}.");
        }

        return await db.RouteWaypoints.AsNoTracking()
            .Where(w => w.CategoryId == categoryId)
            .OrderBy(w => w.SequenceNumber)
            .Select(WaypointMappings.ToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<WaypointDto>> CreateAsync(int categoryId, int organiserId, WaypointRequest request, CancellationToken cancellationToken = default)
    {
        var category = await db.EventCategories.Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Errors.NotFound($"There is no category with id {categoryId}.");
        }

        if (await CheckAsync(category, organiserId, request, exceptWaypointId: null, cancellationToken) is { } refused)
        {
            return refused;
        }

        var waypoint = new RouteWaypoint { CategoryId = categoryId };
        request.ApplyTo(waypoint);
        db.RouteWaypoints.Add(waypoint);
        await db.SaveChangesAsync(cancellationToken);

        return waypoint.ToWaypointDto();
    }

    public async Task<ServiceResult<WaypointDto>> UpdateAsync(int waypointId, int organiserId, WaypointRequest request, CancellationToken cancellationToken = default)
    {
        var waypoint = await db.RouteWaypoints.Include(w => w.Category).ThenInclude(c => c.Event)
            .SingleOrDefaultAsync(w => w.WaypointId == waypointId, cancellationToken);
        if (waypoint is null)
        {
            return Errors.NotFound($"There is no waypoint with id {waypointId}.");
        }

        if (await CheckAsync(waypoint.Category, organiserId, request, exceptWaypointId: waypointId, cancellationToken) is { } refused)
        {
            return refused;
        }

        request.ApplyTo(waypoint);
        await db.SaveChangesAsync(cancellationToken);

        return waypoint.ToWaypointDto();
    }

    public async Task<ServiceResult> DeleteAsync(int waypointId, int organiserId, CancellationToken cancellationToken = default)
    {
        var waypoint = await db.RouteWaypoints.Include(w => w.Category).ThenInclude(c => c.Event)
            .SingleOrDefaultAsync(w => w.WaypointId == waypointId, cancellationToken);
        if (waypoint is null)
        {
            return Errors.NotFound($"There is no waypoint with id {waypointId}.");
        }

        if (Ownership.RequireOwner(waypoint.Category.Event, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        db.RouteWaypoints.Remove(waypoint);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<ServiceError?> CheckAsync(EventCategory category, int organiserId, WaypointRequest request,
        int? exceptWaypointId, CancellationToken cancellationToken)
    {
        if (Ownership.RequireOwner(category.Event, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        if (request.DistanceFromStartKm > category.DistanceKm)
        {
            return Errors.Validation("distanceFromStartKm",
                $"The route is {category.DistanceKm:0.##} km long, so a waypoint cannot be {request.DistanceFromStartKm:0.##} km from the start.");
        }

        var sequenceTaken = await db.RouteWaypoints.AnyAsync(w => w.CategoryId == category.CategoryId
                                                                  && w.SequenceNumber == request.SequenceNumber
                                                                  && w.WaypointId != exceptWaypointId, cancellationToken);
        return sequenceTaken
            ? Errors.Conflict($"Sequence number {request.SequenceNumber} is already used on this route.")
            : null;
    }
}
