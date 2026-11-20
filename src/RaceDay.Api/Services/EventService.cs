using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Services;

public interface IEventService
{
    Task<ServiceResult<PagedResult<EventSummaryDto>>> BrowseAsync(EventQuery query, CancellationToken cancellationToken = default);
    Task<ServiceResult<EventDetailDto>> GetAsync(int eventId, int? callerId, CancellationToken cancellationToken = default);
    Task<ServiceResult<EventDetailDto>> CreateAsync(int organiserId, CreateEventRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<EventDetailDto>> UpdateAsync(int eventId, int organiserId, EventRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<EventDetailDto>> ChangeStatusAsync(int eventId, int organiserId, string status, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int eventId, int organiserId, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<EventSummaryDto>>> ListMineAsync(int organiserId, MyEventsQuery query, CancellationToken cancellationToken = default);
    Task<ServiceResult<EventDetailDto>> SetBannerAsync(int eventId, int organiserId, ImageUpload upload, CancellationToken cancellationToken = default);
    Task<ServiceResult> RemoveBannerAsync(int eventId, int organiserId, CancellationToken cancellationToken = default);
}

public sealed class EventService(RaceDayDbContext db, TimeProvider clock, IImageService images) : IEventService
{
    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        [EventStatuses.Draft] = [EventStatuses.Published, EventStatuses.Cancelled],
        [EventStatuses.Published] = [EventStatuses.Cancelled, EventStatuses.Completed],
        [EventStatuses.Cancelled] = [],
        [EventStatuses.Completed] = []
    };

    public async Task<ServiceResult<PagedResult<EventSummaryDto>>> BrowseAsync(EventQuery query, CancellationToken cancellationToken = default)
    {
        var events = db.Events.AsNoTracking().Where(e => e.EventStatus != EventStatuses.Draft);

        if (DomainValue.Canonical(query.Province, Provinces.All) is { } province)
        {
            events = events.Where(e => e.Province == province);
        }

        if (DomainValue.Canonical(query.Discipline, Disciplines.All) is { } discipline)
        {
            events = events.Where(e => e.Categories.Any(c => c.Discipline == discipline));
        }

        if (query.FromDate is { } from)
        {
            events = events.Where(e => e.EventDate >= from);
        }

        if (query.ToDate is { } to)
        {
            events = events.Where(e => e.EventDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            events = events.Where(e => e.EventName.ToLower().Contains(term)
                                       || e.City.ToLower().Contains(term)
                                       || e.VenueName.ToLower().Contains(term)
                                       || (e.Description != null && e.Description.ToLower().Contains(term)));
        }

        var page = await events
            .OrderBy(e => e.EventDate).ThenBy(e => e.EventName)
            .Select(EventMappings.ToSummary)
            .ToPagedResultAsync(query, cancellationToken);

        return WithReadableBanners(page);
    }

    public async Task<ServiceResult<EventDetailDto>> GetAsync(int eventId, int? callerId, CancellationToken cancellationToken = default)
    {
        var detail = await db.Events.AsNoTracking()
            .Where(e => e.EventId == eventId)
            .VisibleTo(callerId)
            .Select(EventMappings.ToDetail)
            .SingleOrDefaultAsync(cancellationToken);

        return detail is null ? Errors.NotFound($"There is no event with id {eventId}.") : WithReadableBanner(detail);
    }

    public async Task<ServiceResult<EventDetailDto>> CreateAsync(int organiserId, CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EventDate < clock.Today())
        {
            return Errors.Validation("eventDate", "The event date cannot be in the past.");
        }

        var categories = request.Categories ?? [];
        if (categories.Select(c => c.CategoryName.Trim().ToLowerInvariant()).Distinct().Count() != categories.Count)
        {
            return Errors.Validation("categories", "Each category name must be unique within the event.");
        }

        if (await NameTakenAsync(request.EventName, request.EventDate!.Value, exceptEventId: null, cancellationToken))
        {
            return Errors.Conflict($"There is already an event called '{request.EventName.Trim()}' on {request.EventDate:yyyy-MM-dd}.");
        }

        var ev = new Event
        {
            OrganiserId = organiserId,
            EventStatus = EventStatuses.Draft,
            CreatedAt = clock.UtcNowSeconds()
        };
        Apply(ev, request);
        foreach (var category in categories)
        {
            ev.Categories.Add(category.ToEntity(eventId: 0));
        }

        db.Events.Add(ev);
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(ev.EventId, organiserId, cancellationToken);
    }

    public async Task<ServiceResult<EventDetailDto>> UpdateAsync(int eventId, int organiserId, EventRequest request, CancellationToken cancellationToken = default)
    {
        var ev = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (ev is null)
        {
            return Errors.NotFound($"There is no event with id {eventId}.");
        }

        if (Ownership.RequireOwner(ev, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        if (ev.EventStatus is EventStatuses.Cancelled or EventStatuses.Completed)
        {
            return Errors.Conflict($"This event is {ev.EventStatus} and can no longer be edited.");
        }

        if (request.EventDate != ev.EventDate && request.EventDate < clock.Today())
        {
            return Errors.Validation("eventDate", "The event cannot be moved to a date in the past.");
        }

        if (await NameTakenAsync(request.EventName, request.EventDate!.Value, exceptEventId: eventId, cancellationToken))
        {
            return Errors.Conflict($"There is already an event called '{request.EventName.Trim()}' on {request.EventDate:yyyy-MM-dd}.");
        }

        Apply(ev, request);
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(eventId, organiserId, cancellationToken);
    }

    public async Task<ServiceResult<EventDetailDto>> ChangeStatusAsync(int eventId, int organiserId, string status, CancellationToken cancellationToken = default)
    {
        var target = DomainValue.Canonical(status, EventStatuses.All);
        if (target is null)
        {
            return Errors.Validation("status", $"Status must be one of: {string.Join(", ", EventStatuses.All)}.");
        }

        var ev = await db.Events.Include(e => e.Categories).SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (ev is null)
        {
            return Errors.NotFound($"There is no event with id {eventId}.");
        }

        if (Ownership.RequireOwner(ev, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        if (ev.EventStatus == target)
        {
            return Errors.Conflict($"The event is already {target}.");
        }

        if (!AllowedTransitions[ev.EventStatus].Contains(target))
        {
            return Errors.Conflict($"An event cannot go from {ev.EventStatus} to {target}.");
        }

        if (target == EventStatuses.Published && ev.Categories.Count == 0)
        {
            return Errors.Conflict("An event with no categories cannot be published. Add at least one category first.");
        }

        if (target == EventStatuses.Completed && ev.EventDate > clock.Today())
        {
            return Errors.Conflict("An event cannot be completed before its event date.");
        }

        ev.EventStatus = target;
        await db.SaveChangesAsync(cancellationToken);

        return await GetAsync(eventId, organiserId, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(int eventId, int organiserId, CancellationToken cancellationToken = default)
    {
        var ev = await db.Events
            .Include(e => e.WeatherSnapshots)
            .Include(e => e.Categories).ThenInclude(c => c.Waypoints)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments).ThenInclude(en => en.Result)
            .SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (ev is null)
        {
            return Errors.NotFound($"There is no event with id {eventId}.");
        }

        if (Ownership.RequireOwner(ev, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        if (ev.Categories.SelectMany(c => c.Enrolments).Any(en => en.EnrolmentStatus == EnrolmentStatuses.Confirmed))
        {
            return Errors.Conflict("This event has confirmed entries, so it cannot be deleted. Cancel it instead.");
        }

        var banner = ev.BannerImageUrl;
        db.Events.Remove(ev);
        await db.SaveChangesAsync(cancellationToken);
        await images.RemoveAsync(banner, cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<PagedResult<EventSummaryDto>>> ListMineAsync(int organiserId, MyEventsQuery query, CancellationToken cancellationToken = default)
    {
        var events = db.Events.AsNoTracking().Where(e => e.OrganiserId == organiserId);

        if (DomainValue.Canonical(query.Status, EventStatuses.All) is { } status)
        {
            events = events.Where(e => e.EventStatus == status);
        }

        var page = await events
            .OrderByDescending(e => e.EventDate).ThenBy(e => e.EventName)
            .Select(EventMappings.ToSummary)
            .ToPagedResultAsync(query, cancellationToken);

        return WithReadableBanners(page);
    }

    public async Task<ServiceResult<EventDetailDto>> SetBannerAsync(int eventId, int organiserId, ImageUpload upload, CancellationToken cancellationToken = default)
    {
        var (ev, refused) = await LoadEditableAsync(eventId, organiserId, cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        var saved = await images.SaveAsync(ImageKind.EventBanner, $"events/{eventId}", upload, cancellationToken);
        if (!saved.Succeeded)
        {
            return saved.Error;
        }

        var previous = ev!.BannerImageUrl;
        ev.BannerImageUrl = saved.Value;
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
        return await GetAsync(eventId, organiserId, cancellationToken);
    }

    public async Task<ServiceResult> RemoveBannerAsync(int eventId, int organiserId, CancellationToken cancellationToken = default)
    {
        var (ev, refused) = await LoadEditableAsync(eventId, organiserId, cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        var previous = ev!.BannerImageUrl;
        if (previous is null)
        {
            return ServiceResult.Success();
        }

        ev.BannerImageUrl = null;
        await db.SaveChangesAsync(cancellationToken);
        await images.RemoveAsync(previous, cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<(Event? Event, ServiceError? Refused)> LoadEditableAsync(int eventId, int organiserId, CancellationToken cancellationToken)
    {
        var ev = await db.Events.SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (ev is null)
        {
            return (null, Errors.NotFound($"There is no event with id {eventId}."));
        }

        if (Ownership.RequireOwner(ev, organiserId) is { } forbidden)
        {
            return (null, forbidden);
        }

        return ev.EventStatus is EventStatuses.Cancelled or EventStatuses.Completed
            ? (null, Errors.Conflict($"This event is {ev.EventStatus} and can no longer be edited."))
            : (ev, null);
    }

    private T WithReadableBanner<T>(T summary) where T : EventSummaryDto
    {
        summary.BannerImageUrl = images.ReadUrl(summary.BannerImageUrl);
        return summary;
    }

    private PagedResult<EventSummaryDto> WithReadableBanners(PagedResult<EventSummaryDto> page)
    {
        foreach (var summary in page.Items)
        {
            WithReadableBanner(summary);
        }

        return page;
    }

    private Task<bool> NameTakenAsync(string eventName, DateOnly eventDate, int? exceptEventId, CancellationToken cancellationToken)
    {
        var name = eventName.Trim().ToLower();
        return db.Events.AnyAsync(e => e.EventName.ToLower() == name
                                       && e.EventDate == eventDate
                                       && e.EventId != exceptEventId, cancellationToken);
    }

    private static void Apply(Event ev, EventRequest request)
    {
        ev.EventName = request.EventName.Trim();
        ev.Description = UserRules.NormaliseOptional(request.Description);
        ev.EventDate = request.EventDate!.Value;
        ev.StartTime = request.StartTime!.Value;
        ev.Province = DomainValue.Canonical(request.Province, Provinces.All)!;
        ev.City = request.City.Trim();
        ev.VenueName = request.VenueName.Trim();
        ev.Latitude = request.Latitude!.Value;
        ev.Longitude = request.Longitude!.Value;
        ev.RegistrationOpens = request.RegistrationOpens!.Value;
        ev.RegistrationCloses = request.RegistrationCloses!.Value;
    }
}
