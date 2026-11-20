using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Domain;
using RaceDay.Api.Dtos;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

public interface IResultService
{
    Task<ServiceResult<ResultDto>> CaptureAsync(int enrolmentId, int organiserId, ResultRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<ResultDto>> UpdateAsync(int resultId, int organiserId, ResultRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(int resultId, int organiserId, CancellationToken cancellationToken = default);
    Task<ServiceResult<ResultDto>> GetAsync(int resultId, CancellationToken cancellationToken = default);
    Task<ServiceResult<BulkImportResponse>> ImportAsync(int categoryId, int organiserId, BulkResultsRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<LeaderboardEntryDto>>> LeaderboardAsync(int categoryId, LeaderboardQuery query, CancellationToken cancellationToken = default);
    Task<ServiceResult<PagedResult<ResultDto>>> ListMineAsync(int participantId, MyResultsQuery query, CancellationToken cancellationToken = default);
    Task<ServiceResult<EventResultsDto>> EventResultsAsync(int eventId, CancellationToken cancellationToken = default);
}

public sealed class ResultService(RaceDayDbContext db, TimeProvider clock) : IResultService
{
    public async Task<ServiceResult<ResultDto>> CaptureAsync(int enrolmentId, int organiserId, ResultRequest request, CancellationToken cancellationToken = default)
    {
        var enrolment = await db.Enrolments
            .Include(e => e.Result)
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .SingleOrDefaultAsync(e => e.EnrolmentId == enrolmentId, cancellationToken);
        if (enrolment is null)
        {
            return Errors.NotFound($"There is no enrolment with id {enrolmentId}.");
        }

        if (Ownership.RequireOwner(enrolment.Category.Event, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        if (enrolment.Result is not null)
        {
            return Errors.Conflict($"A result already exists for this entry (result {enrolment.Result.ResultId}). Use PUT /api/results/{enrolment.Result.ResultId} to correct it.");
        }

        if (CheckCanRecord(enrolment) is { } refused)
        {
            return refused;
        }

        var result = new Result { EnrolmentId = enrolmentId };
        if (Apply(request, result, enrolment.Category) is { } invalid)
        {
            return invalid;
        }

        result.CapturedByUserId = organiserId;
        result.CapturedAt = clock.UtcNowSeconds();
        db.Results.Add(result);
        await db.SaveChangesAsync(cancellationToken);

        return await ProjectAsync(result.ResultId, cancellationToken);
    }

    public async Task<ServiceResult<ResultDto>> UpdateAsync(int resultId, int organiserId, ResultRequest request, CancellationToken cancellationToken = default)
    {
        var result = await db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .SingleOrDefaultAsync(r => r.ResultId == resultId, cancellationToken);
        if (result is null)
        {
            return Errors.NotFound($"There is no result with id {resultId}.");
        }

        if (Ownership.RequireOwner(result.Enrolment.Category.Event, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        if (Apply(request, result, result.Enrolment.Category) is { } invalid)
        {
            return invalid;
        }

        result.CapturedByUserId = organiserId;
        result.CapturedAt = clock.UtcNowSeconds();
        await db.SaveChangesAsync(cancellationToken);

        return await ProjectAsync(resultId, cancellationToken);
    }

    public async Task<ServiceResult> DeleteAsync(int resultId, int organiserId, CancellationToken cancellationToken = default)
    {
        var result = await db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
            .SingleOrDefaultAsync(r => r.ResultId == resultId, cancellationToken);
        if (result is null)
        {
            return Errors.NotFound($"There is no result with id {resultId}.");
        }

        if (Ownership.RequireOwner(result.Enrolment.Category.Event, organiserId) is { } forbidden)
        {
            return forbidden;
        }

        db.Results.Remove(result);
        await db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<ResultDto>> GetAsync(int resultId, CancellationToken cancellationToken = default)
    {
        var result = await db.Results.AsNoTracking()
            .Where(r => r.ResultId == resultId)
            .Select(ResultMappings.ToDto)
            .SingleOrDefaultAsync(cancellationToken);

        return result is null ? Errors.NotFound($"There is no result with id {resultId}.") : result;
    }

    public async Task<ServiceResult<BulkImportResponse>> ImportAsync(int categoryId, int organiserId, BulkResultsRequest request, CancellationToken cancellationToken = default)
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

        if (category.Event.EventDate > clock.Today())
        {
            return Errors.Conflict("The event has not been run yet, so results cannot be imported.");
        }

        var entries = await db.Enrolments
            .Include(e => e.Result)
            .Include(e => e.Participant)
            .Where(e => e.CategoryId == categoryId)
            .ToListAsync(cancellationToken);
        var byRaceNumber = entries.ToDictionary(e => e.RaceNumber.ToUpperInvariant());

        var response = new BulkImportResponse();
        var seen = new HashSet<string>();
        var now = clock.UtcNowSeconds();

        foreach (var row in request.Results)
        {
            var raceNumber = row.RaceNumber.Trim().ToUpperInvariant();
            string? error = null;

            if (!seen.Add(raceNumber))
            {
                error = "This race number appears more than once in the file.";
            }
            else if (!byRaceNumber.TryGetValue(raceNumber, out var enrolment))
            {
                error = "No entry in this category has that race number.";
            }
            else if (enrolment.Result is not null)
            {
                response.Skipped++;
                continue;
            }
            else if (enrolment.EnrolmentStatus != EnrolmentStatuses.Confirmed)
            {
                error = $"The entry is {enrolment.EnrolmentStatus}, not Confirmed.";
            }
            else if (BuildImportedResult(row, category.StartTime, out var result) is { } rowError)
            {
                error = rowError;
            }
            else
            {
                result!.EnrolmentId = enrolment.EnrolmentId;
                result.CapturedByUserId = organiserId;
                result.CapturedAt = now;
                enrolment.Result = result;
                db.Results.Add(result);
                response.Imported++;
                continue;
            }

            response.Errors.Add(new BulkImportError { RaceNumber = row.RaceNumber, Error = error });
        }

        if (response.Imported > 0)
        {
            var ranked = entries.Where(e => e.Result is not null)
                .Select(e => new ResultRanking.Entrant(e.Result!, e.Participant.Gender, e.Participant.DateOfBirth))
                .ToList();
            ResultRanking.Recalculate(ranked, category.Event.EventDate);
            await db.SaveChangesAsync(cancellationToken);
        }

        return response;
    }

    public async Task<ServiceResult<PagedResult<LeaderboardEntryDto>>> LeaderboardAsync(int categoryId, LeaderboardQuery query, CancellationToken cancellationToken = default)
    {
        var visible = await db.EventCategories.AnyAsync(
            c => c.CategoryId == categoryId && c.Event.EventStatus != EventStatuses.Draft, cancellationToken);
        if (!visible)
        {
            return Errors.NotFound($"There is no category with id {categoryId}.");
        }

        var results = db.Results.AsNoTracking().Where(r => r.Enrolment.CategoryId == categoryId);
        var gender = DomainValue.Canonical(query.Gender, Genders.All);
        if (gender is not null)
        {
            results = results.Where(r => r.Enrolment.Participant.Gender == gender);
        }

        return await results
            .InLeaderboardOrder(byGender: gender is not null)
            .Select(LeaderboardMappings.ToEntry)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<ServiceResult<PagedResult<ResultDto>>> ListMineAsync(int participantId, MyResultsQuery query, CancellationToken cancellationToken = default)
    {
        var results = db.Results.AsNoTracking().Where(r => r.Enrolment.ParticipantId == participantId);

        if (query.FromDate is { } from)
        {
            results = results.Where(r => r.Enrolment.Category.Event.EventDate >= from);
        }

        if (query.ToDate is { } to)
        {
            results = results.Where(r => r.Enrolment.Category.Event.EventDate <= to);
        }

        if (DomainValue.Canonical(query.Discipline, Disciplines.All) is { } discipline)
        {
            results = results.Where(r => r.Enrolment.Category.Discipline == discipline);
        }

        return await results
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate).ThenBy(r => r.ResultId)
            .Select(ResultMappings.ToDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<ServiceResult<EventResultsDto>> EventResultsAsync(int eventId, CancellationToken cancellationToken = default)
    {
        var ev = await db.Events.AsNoTracking()
            .Where(e => e.EventId == eventId && e.EventStatus != EventStatuses.Draft)
            .Select(e => new EventResultsDto
            {
                EventId = e.EventId,
                EventName = e.EventName,
                EventDate = e.EventDate,
                EventStatus = e.EventStatus,
                Categories = e.Categories
                    .OrderByDescending(c => c.DistanceKm).ThenBy(c => c.CategoryName)
                    .Select(c => new CategoryResultsDto
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        Discipline = c.Discipline,
                        DistanceKm = c.DistanceKm
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (ev is null)
        {
            return Errors.NotFound($"There is no event with id {eventId}.");
        }

        var rows = await db.Results.AsNoTracking()
            .Where(r => r.Enrolment.Category.EventId == eventId)
            .InLeaderboardOrder(byGender: false)
            .Select(LeaderboardMappings.ToEntry)
            .ToListAsync(cancellationToken);

        foreach (var category in ev.Categories)
        {
            category.Results = rows.Where(r => r.CategoryId == category.CategoryId).ToList();
        }

        return ev;
    }

    private static string? BuildImportedResult(BulkResultRow row, TimeOnly start, out Result? result)
    {
        result = null;
        var status = DomainValue.Canonical(row.ResultStatus, ResultStatuses.All)!;

        if (status != ResultStatuses.Finished)
        {
            if (row.FinishTime is not null || row.ChipTimeSeconds is not null)
            {
                return $"A {status} result cannot have a finish time.";
            }

            result = new Result { ResultStatus = status, StartTime = start };
            return null;
        }

        if (row.FinishTime is null && row.ChipTimeSeconds is null)
        {
            return "A finisher needs a finish time or a chip time.";
        }

        var chipSeconds = row.ChipTimeSeconds ?? (int)(row.FinishTime!.Value - start).TotalSeconds;
        if (chipSeconds <= 0 || row.FinishTime <= start)
        {
            return "The finish time must be after the category's start time.";
        }

        result = new Result
        {
            ResultStatus = status,
            StartTime = start,
            ChipTimeSeconds = chipSeconds,
            FinishTime = row.FinishTime ?? start.Add(TimeSpan.FromSeconds(chipSeconds)),
            OverallPosition = int.MaxValue
        };
        return null;
    }

    private ServiceError? CheckCanRecord(Enrolment enrolment)
    {
        if (enrolment.EnrolmentStatus != EnrolmentStatuses.Confirmed)
        {
            return Errors.Conflict($"Only confirmed entries can be given a result; this entry is {enrolment.EnrolmentStatus}.");
        }

        return enrolment.Category.Event.EventDate > clock.Today()
            ? Errors.Conflict("The event has not been run yet, so results cannot be captured.")
            : null;
    }

    private static ServiceError? Apply(ResultRequest request, Result result, EventCategory category)
    {
        var status = DomainValue.Canonical(request.ResultStatus, ResultStatuses.All)!;
        var start = request.StartTime ?? category.StartTime;

        result.ResultStatus = status;
        result.StartTime = start;
        result.GenderPosition = request.GenderPosition;
        result.AgeGroupPosition = request.AgeGroupPosition;

        if (status != ResultStatuses.Finished)
        {
            result.FinishTime = null;
            result.ChipTimeSeconds = null;
            result.OverallPosition = null;
            return null;
        }

        var chipSeconds = request.ChipTimeSeconds ?? (int)(request.FinishTime!.Value - start).TotalSeconds;
        if (request.FinishTime <= start || chipSeconds <= 0)
        {
            return Errors.Validation("finishTime", "The finish time must be after the start time.");
        }

        result.FinishTime = request.FinishTime;
        result.ChipTimeSeconds = chipSeconds;
        result.OverallPosition = request.OverallPosition;
        return null;
    }

    private async Task<ServiceResult<ResultDto>> ProjectAsync(int resultId, CancellationToken cancellationToken) =>
        await db.Results.AsNoTracking()
            .Where(r => r.ResultId == resultId)
            .Select(ResultMappings.ToDto)
            .SingleAsync(cancellationToken);
}
