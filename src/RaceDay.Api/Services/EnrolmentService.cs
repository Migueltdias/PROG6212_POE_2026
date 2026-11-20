using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public interface IEnrolmentService
{
    Task<ServiceResult<EnrolmentDto>> EnterAsync(int categoryId, int participantId, EnrolmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<EnrolmentDto>>> ListMineAsync(int participantId, MyEnrolmentsQuery query, CancellationToken cancellationToken = default);
    Task<ServiceResult<EnrolmentDto>> GetAsync(int enrolmentId, int callerId, string? callerRole, CancellationToken cancellationToken = default);
    Task<ServiceResult<EnrolmentDto>> UpdateAsync(int enrolmentId, int participantId, EnrolmentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<EnrolmentDto>> RecordPaymentAsync(int enrolmentId, int participantId, PaymentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> WithdrawAsync(int enrolmentId, int participantId, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<EnrolmentDto>>> CategoryStartListAsync(int categoryId, int organiserId, StartListQuery query, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<EnrolmentDto>>> EventStartListAsync(int eventId, int organiserId, EventStartListQuery query, CancellationToken cancellationToken = default);
}

public sealed class EnrolmentService(RaceDayDbContext db, TimeProvider clock) : IEnrolmentService
{
    public async Task<ServiceResult<EnrolmentDto>> EnterAsync(int categoryId, int participantId, EnrolmentRequest request, CancellationToken cancellationToken = default)
    {
        var category = await db.EventCategories.Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken);
        if (category is null || category.Event.EventStatus == EventStatuses.Draft)
        {
            return Errors.NotFound($"There is no category with id {categoryId}.");
        }

        var ev = category.Event;
        var today = clock.Today();
        if (ev.EventStatus != EventStatuses.Published)
        {
            return Errors.Conflict($"Entries are closed because the event is {ev.EventStatus}.");
        }

        if (today < ev.RegistrationOpens)
        {
            return Errors.Conflict($"Entries for this event open on {ev.RegistrationOpens:yyyy-MM-dd}.");
        }

        if (today > ev.RegistrationCloses)
        {
            return Errors.Conflict($"Entries for this event closed on {ev.RegistrationCloses:yyyy-MM-dd}.");
        }

        var existing = await db.Enrolments.SingleOrDefaultAsync(
            e => e.ParticipantId == participantId && e.CategoryId == categoryId, cancellationToken);
        if (existing is not null && EnrolmentStatuses.Active.Contains(existing.EnrolmentStatus))
        {
            return Errors.Conflict($"You have already entered this category (race number {existing.RaceNumber}).");
        }

        var participant = await db.Users.SingleAsync(u => u.UserId == participantId, cancellationToken);
        var ageOnRaceDay = TimeProviderExtensions.AgeOn(participant.DateOfBirth, ev.EventDate);
        if (ageOnRaceDay < category.MinimumAge)
        {
            return Errors.Conflict($"The minimum age for this category is {category.MinimumAge}; you will be {ageOnRaceDay} on race day.");
        }

        var entered = await db.Enrolments.CountAsync(
            e => e.CategoryId == categoryId && EnrolmentStatuses.Active.Contains(e.EnrolmentStatus), cancellationToken);
        if (entered >= category.MaxParticipants)
        {
            return Errors.Conflict($"This category is full ({category.MaxParticipants} entries).");
        }

        Enrolment enrolment;
        if (existing is not null)
        {
            enrolment = existing;
        }
        else
        {
            var usedNumbers = await db.Enrolments.Where(e => e.CategoryId == categoryId)
                .Select(e => e.RaceNumber).ToListAsync(cancellationToken);
            enrolment = new Enrolment
            {
                ParticipantId = participantId,
                CategoryId = categoryId,
                RaceNumber = RaceNumbers.Next(RaceNumbers.PrefixFor(ev.EventName), usedNumbers),
                AmountPaid = 0m
            };
            db.Enrolments.Add(enrolment);
        }

        request.ApplyTo(enrolment);
        enrolment.EnrolmentDate = clock.UtcNowSeconds();
        enrolment.EnrolmentStatus = enrolment.AmountPaid >= category.EntryFee
            ? EnrolmentStatuses.Confirmed
            : EnrolmentStatuses.Pending;

        await db.SaveChangesAsync(cancellationToken);
        return await ProjectAsync(enrolment.EnrolmentId, cancellationToken);
    }

    public async Task<ServiceResult<PagedResult<EnrolmentDto>>> ListMineAsync(int participantId, MyEnrolmentsQuery query, CancellationToken cancellationToken = default)
    {
        var enrolments = db.Enrolments.AsNoTracking().Where(e => e.ParticipantId == participantId);

        if (DomainValue.Canonical(query.Status, EnrolmentStatuses.All) is { } status)
        {
            enrolments = enrolments.Where(e => e.EnrolmentStatus == status);
        }

        var today = clock.Today();
        enrolments = query.Upcoming switch
        {
            true => enrolments.Where(e => e.Category.Event.EventDate >= today)
                .OrderBy(e => e.Category.Event.EventDate).ThenBy(e => e.EnrolmentId),
            false => enrolments.Where(e => e.Category.Event.EventDate < today)
                .OrderByDescending(e => e.Category.Event.EventDate).ThenBy(e => e.EnrolmentId),
            null => enrolments.OrderByDescending(e => e.Category.Event.EventDate).ThenBy(e => e.EnrolmentId)
        };

        return await enrolments.Select(EnrolmentMappings.ToDto).ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<ServiceResult<EnrolmentDto>> GetAsync(int enrolmentId, int callerId, string? callerRole, CancellationToken cancellationToken = default)
    {
        var owners = await db.Enrolments.AsNoTracking()
            .Where(e => e.EnrolmentId == enrolmentId)
            .Select(e => new { e.ParticipantId, e.Category.Event.OrganiserId })
            .SingleOrDefaultAsync(cancellationToken);
        if (owners is null)
        {
            return Errors.NotFound($"There is no enrolment with id {enrolmentId}.");
        }

        var allowed = (callerRole == RoleNames.Participant && owners.ParticipantId == callerId)
                      || (callerRole == RoleNames.Organiser && owners.OrganiserId == callerId);
        if (!allowed)
        {
            return Errors.Forbidden("This entry belongs to someone else.");
        }

        return await ProjectAsync(enrolmentId, cancellationToken);
    }

    public async Task<ServiceResult<EnrolmentDto>> UpdateAsync(int enrolmentId, int participantId, EnrolmentRequest request, CancellationToken cancellationToken = default)
    {
        var (enrolment, refused) = await LoadOwnOpenEntryAsync(enrolmentId, participantId, cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        request.ApplyTo(enrolment!);
        await db.SaveChangesAsync(cancellationToken);
        return await ProjectAsync(enrolmentId, cancellationToken);
    }

    public async Task<ServiceResult<EnrolmentDto>> RecordPaymentAsync(int enrolmentId, int participantId, PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var (enrolment, refused) = await LoadOwnOpenEntryAsync(enrolmentId, participantId, cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        if (enrolment!.EnrolmentStatus == EnrolmentStatuses.Confirmed)
        {
            return Errors.Conflict("This entry is already paid for and confirmed.");
        }

        var fee = enrolment.Category.EntryFee;
        if (request.AmountPaid != fee)
        {
            return Errors.Validation("amountPaid", $"The entry fee is R{fee:0.00}; the amount paid must match it exactly.");
        }

        enrolment.AmountPaid = request.AmountPaid.Value;
        enrolment.PaymentReference = request.PaymentReference.Trim();
        enrolment.EnrolmentStatus = EnrolmentStatuses.Confirmed;
        await db.SaveChangesAsync(cancellationToken);
        return await ProjectAsync(enrolmentId, cancellationToken);
    }

    public async Task<ServiceResult> WithdrawAsync(int enrolmentId, int participantId, CancellationToken cancellationToken = default)
    {
        var (enrolment, refused) = await LoadOwnOpenEntryAsync(enrolmentId, participantId, cancellationToken);
        if (refused is not null)
        {
            return refused;
        }

        enrolment!.EnrolmentStatus = EnrolmentStatuses.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<PagedResult<EnrolmentDto>>> CategoryStartListAsync(int categoryId, int organiserId, StartListQuery query, CancellationToken cancellationToken = default)
    {
        var category = await db.EventCategories.Include(c => c.Event)
            .SingleOrDefaultAsync(c => c.CategoryId == categoryId, cancellationToken);
        if (category is null)
        {
            return Errors.NotFound($"There is no category with id {categoryId}.");
        }

        if (Ownership.RequireOwner(category.Event, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        var entries = FilterByStatus(db.Enrolments.AsNoTracking().Where(e => e.CategoryId == categoryId), query.Status);
        return await entries.OrderBy(e => e.RaceNumber)
            .Select(EnrolmentMappings.ToDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<ServiceResult<PagedResult<EnrolmentDto>>> EventStartListAsync(int eventId, int organiserId, EventStartListQuery query, CancellationToken cancellationToken = default)
    {
        var ev = await db.Events.Include(e => e.Categories).SingleOrDefaultAsync(e => e.EventId == eventId, cancellationToken);
        if (ev is null)
        {
            return Errors.NotFound($"There is no event with id {eventId}.");
        }

        if (Ownership.RequireOwner(ev, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        var entries = db.Enrolments.AsNoTracking().Where(e => e.Category.EventId == eventId);
        if (query.CategoryId is { } categoryId)
        {
            if (ev.Categories.All(c => c.CategoryId != categoryId))
            {
                return Errors.Validation("categoryId", $"Category {categoryId} is not part of this event.");
            }

            entries = entries.Where(e => e.CategoryId == categoryId);
        }

        return await FilterByStatus(entries, query.Status)
            .OrderByDescending(e => e.Category.DistanceKm).ThenBy(e => e.RaceNumber)
            .Select(EnrolmentMappings.ToDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    private async Task<(Enrolment? Enrolment, ServiceError? Refused)> LoadOwnOpenEntryAsync(
        int enrolmentId, int participantId, CancellationToken cancellationToken)
    {
        var enrolment = await db.Enrolments.Include(e => e.Category).ThenInclude(c => c.Event)
            .SingleOrDefaultAsync(e => e.EnrolmentId == enrolmentId, cancellationToken);
        if (enrolment is null)
        {
            return (null, Errors.NotFound($"There is no enrolment with id {enrolmentId}."));
        }

        if (enrolment.ParticipantId != participantId)
        {
            return (null, Errors.Forbidden("This entry belongs to someone else."));
        }

        if (enrolment.EnrolmentStatus is EnrolmentStatuses.Cancelled or EnrolmentStatuses.Withdrawn)
        {
            return (null, Errors.Conflict("This entry has been withdrawn. Enter the category again to reinstate it."));
        }

        var ev = enrolment.Category.Event;
        if (ev.EventStatus == EventStatuses.Completed || ev.EventDate <= clock.Today())
        {
            return (null, Errors.Conflict("The event has already been run, so this entry can no longer be changed."));
        }

        return (enrolment, null);
    }

    private static IQueryable<Enrolment> FilterByStatus(IQueryable<Enrolment> entries, string? status) =>
        DomainValue.Canonical(status, EnrolmentStatuses.All) is { } canonical
            ? entries.Where(e => e.EnrolmentStatus == canonical)
            : entries;

    private async Task<ServiceResult<EnrolmentDto>> ProjectAsync(int enrolmentId, CancellationToken cancellationToken) =>
        await db.Enrolments.AsNoTracking()
            .Where(e => e.EnrolmentId == enrolmentId)
            .Select(EnrolmentMappings.ToDto)
            .SingleAsync(cancellationToken);
}
