using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Unit;

public class EnrolmentServiceTests : IDisposable
{
    private readonly RaceDayDbContext _db = UnitDatabase.Create();
    private readonly FixedTimeProvider _clock = new(RaceDayApiFactory.Today);
    private readonly EnrolmentService _service;

    public EnrolmentServiceTests() => _service = new EnrolmentService(_db, _clock);

    private static EnrolmentRequest Entry => new()
    {
        TShirtSize = "M",
        EmergencyContactName = "Riaz Patel",
        EmergencyContactPhone = "0836549871"
    };

    [Fact]
    public async Task Entering_records_the_participant_the_category_and_a_new_race_number()
    {
        var result = await _service.EnterAsync(4, 5, Entry);

        Assert.True(result.Succeeded);
        Assert.Equal("SM1121", result.Value!.RaceNumber);
        Assert.Equal(2, result.Value.EventId);
        var row = await _db.Enrolments.SingleAsync(e => e.ParticipantId == 5 && e.CategoryId == 4);
        Assert.Equal("Pending", row.EnrolmentStatus);
        Assert.Equal(0m, row.AmountPaid);
    }

    [Fact]
    public async Task A_second_active_entry_in_the_same_category_is_a_conflict()
    {
        var result = await _service.EnterAsync(4, 4, Entry);

        Assert.Equal(ServiceErrorKind.Conflict, result.Error?.Kind);
    }

    [Fact]
    public async Task Entering_under_the_minimum_age_is_a_conflict()
    {
        var category = await _db.EventCategories.SingleAsync(c => c.CategoryId == 3);
        category.MinimumAge = 40;
        await _db.SaveChangesAsync();

        var result = await _service.EnterAsync(3, 5, Entry);

        Assert.Equal(ServiceErrorKind.Conflict, result.Error?.Kind);
        Assert.Contains("minimum age", result.Error!.Message);
    }

    [Fact]
    public async Task Entering_a_full_category_is_a_conflict()
    {
        var category = await _db.EventCategories.SingleAsync(c => c.CategoryId == 4);
        category.MaxParticipants = 1;
        await _db.SaveChangesAsync();

        var result = await _service.EnterAsync(4, 5, Entry);

        Assert.Equal(ServiceErrorKind.Conflict, result.Error?.Kind);
    }

    [Fact]
    public async Task Entering_after_registration_has_closed_is_a_conflict()
    {
        _clock.Advance(TimeSpan.FromDays(20));

        var result = await _service.EnterAsync(4, 5, Entry);

        Assert.Equal(ServiceErrorKind.Conflict, result.Error?.Kind);
    }

    [Fact]
    public async Task Another_participant_cannot_read_the_entry()
    {
        var result = await _service.GetAsync(12, 3, "Participant");

        Assert.Equal(ServiceErrorKind.Forbidden, result.Error?.Kind);
    }

    [Fact]
    public async Task The_events_organiser_can_read_the_entry()
    {
        var result = await _service.GetAsync(12, 1, "Organiser");

        Assert.True(result.Succeeded);
        Assert.Equal("CT0911", result.Value!.RaceNumber);
    }

    public void Dispose() => _db.Dispose();
}
