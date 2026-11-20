using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class EnrolmentTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task A_Participant_enters_an_event_by_selecting_a_category()
    {
        var (participant, profile) = await factory.RegisterAndLoginAsync();

        var response = await participant.PostAsJsonAsync("/api/categories/5/enrolments", TestData.Entry("s"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var entry = await response.ReadAsync<EnrolmentDto>();
        Assert.Equal(profile.UserId, entry.ParticipantId);
        Assert.Equal(5, entry.CategoryId);
        Assert.Equal(2, entry.EventId);
        Assert.Equal("Soweto Marathon 2026", entry.EventName);
        Assert.Equal("Walk", entry.Discipline);
        Assert.Equal("Pending", entry.EnrolmentStatus);
        Assert.Equal("S", entry.TShirtSize);
        Assert.Equal("SM2002", entry.RaceNumber);
        Assert.Equal($"/api/enrolments/{entry.EnrolmentId}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task The_enrolment_is_recorded_against_the_participant_and_the_selected_category()
    {
        var (participant, profile) = await factory.RegisterAndLoginAsync();

        var entry = await (await participant.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry()))
            .ReadAsync<EnrolmentDto>();

        var row = await factory.WithDbAsync(db => db.Enrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .SingleAsync(e => e.EnrolmentId == entry.EnrolmentId));
        Assert.Equal(profile.UserId, row.ParticipantId);
        Assert.Equal(4, row.CategoryId);
        Assert.Equal(2, row.Category.EventId);
        Assert.Equal(entry.RaceNumber, row.RaceNumber);
    }

    [Fact]
    public async Task The_Organiser_of_the_event_can_view_every_enrolment_for_it()
    {
        var (participant, _) = await factory.RegisterAndLoginAsync();
        var entry = await (await participant.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry()))
            .ReadAsync<EnrolmentDto>();
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var single = await pieter.GetAsync($"/api/enrolments/{entry.EnrolmentId}");
        var eventList = await pieter.GetFromJsonAsync<PagedResult<EnrolmentDto>>("/api/events/2/enrolments?pageSize=100");
        var categoryList = await pieter.GetFromJsonAsync<PagedResult<EnrolmentDto>>("/api/categories/4/enrolments?pageSize=100");

        await single.ShouldHaveStatusAsync(HttpStatusCode.OK);
        Assert.Contains(eventList!.Items, e => e.EnrolmentId == entry.EnrolmentId);
        Assert.Contains(eventList.Items, e => e.RaceNumber == "SM0450");
        Assert.All(eventList.Items, e => Assert.Equal(2, e.EventId));
        Assert.Contains(categoryList!.Items, e => e.EnrolmentId == entry.EnrolmentId);
        Assert.All(categoryList.Items, e => Assert.Equal(4, e.CategoryId));
    }

    [Fact]
    public async Task Race_numbers_are_unique_within_a_category()
    {
        var (first, _) = await factory.RegisterAndLoginAsync();
        var (second, _) = await factory.RegisterAndLoginAsync();

        var a = await (await first.PostAsJsonAsync("/api/categories/6/enrolments", TestData.Entry())).ReadAsync<EnrolmentDto>();
        var b = await (await second.PostAsJsonAsync("/api/categories/6/enrolments", TestData.Entry())).ReadAsync<EnrolmentDto>();

        Assert.StartsWith("CT", a.RaceNumber);
        Assert.NotEqual(a.RaceNumber, b.RaceNumber);
        Assert.Equal(int.Parse(a.RaceNumber[2..]) + 1, int.Parse(b.RaceNumber[2..]));
    }

    [Fact]
    public async Task Entering_the_same_category_twice_returns_409()
    {
        var lerato = await factory.CreateClientForAsync(TestUsers.Lerato);

        var response = await lerato.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        Assert.Contains("already entered", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task An_Organiser_cannot_enter_an_event()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var response = await pieter.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_visitor_must_log_in_to_enter()
    {
        var response = await factory.CreateAnonymousClient().PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Entering_below_the_minimum_age_returns_409()
    {
        var (teenager, _) = await factory.RegisterAndLoginAsync(dateOfBirth: new DateOnly(2012, 6, 1));

        var response = await teenager.PostAsJsonAsync("/api/categories/3/enrolments", TestData.Entry());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        Assert.Contains("minimum age", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task A_full_category_returns_409()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);
        var category = await (await pieter.PostAsJsonAsync("/api/events/2/categories",
            TestData.NewCategory($"Elite Mile {Guid.NewGuid():N}", maxParticipants: 1))).ReadAsync<CategoryDto>();
        var (first, _) = await factory.RegisterAndLoginAsync();
        var (second, _) = await factory.RegisterAndLoginAsync();

        await (await first.PostAsJsonAsync($"/api/categories/{category.CategoryId}/enrolments", TestData.Entry()))
            .ShouldHaveStatusAsync(HttpStatusCode.Created);
        var response = await second.PostAsJsonAsync($"/api/categories/{category.CategoryId}/enrolments", TestData.Entry());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        Assert.Contains("full", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task Entries_to_an_event_that_has_been_run_are_closed()
    {
        var (participant, _) = await factory.RegisterAndLoginAsync();

        var response = await participant.PostAsJsonAsync("/api/categories/2/enrolments", TestData.Entry());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Entries_are_refused_after_registration_closes()
    {
        await using var later = new RaceDayApiFactory();
        later.Clock.Advance(TimeSpan.FromDays(24));
        var (participant, _) = await later.RegisterAndLoginAsync();

        var response = await participant.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        Assert.Contains("closed on 2026-10-20", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task Entries_are_refused_before_registration_opens()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var created = await thandeka.PostAsJsonAsync("/api/events",
            TestData.NewEvent(registrationOpens: "2026-12-01", categories: [TestData.NewCategory()]));
        var ev = await created.ReadAsync<EventDetailDto>();
        await thandeka.PatchAsJsonAsync($"/api/events/{ev.EventId}/status", new { status = "Published" });
        var (participant, _) = await factory.RegisterAndLoginAsync();

        var response = await participant.PostAsJsonAsync($"/api/categories/{ev.Categories[0].CategoryId}/enrolments", TestData.Entry());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        Assert.Contains("open on 2026-12-01", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task A_category_of_a_draft_or_missing_event_returns_404()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var draft = await TestData.CreateEventAsync(thandeka, publish: false, TestData.NewCategory());
        var (participant, _) = await factory.RegisterAndLoginAsync();

        var draftEntry = await participant.PostAsJsonAsync($"/api/categories/{draft.Categories[0].CategoryId}/enrolments", TestData.Entry());
        var missingEntry = await participant.PostAsJsonAsync("/api/categories/9999/enrolments", TestData.Entry());

        await draftEntry.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        await missingEntry.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Invalid_entry_details_return_400()
    {
        var (participant, _) = await factory.RegisterAndLoginAsync();

        var response = await participant.PostAsJsonAsync("/api/categories/4/enrolments",
            new { tShirtSize = "XXXL", emergencyContactName = "", emergencyContactPhone = "call me" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var errors = (await response.ReadValidationProblemAsync()).Errors.Keys;
        Assert.Contains("tShirtSize", errors);
        Assert.Contains("emergencyContactName", errors);
        Assert.Contains("emergencyContactPhone", errors);
    }

    [Fact]
    public async Task A_free_category_is_confirmed_straight_away()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);
        var free = await (await pieter.PostAsJsonAsync("/api/events/2/categories",
            TestData.NewCategory($"Kids Dash {Guid.NewGuid():N}", entryFee: 0m, minimumAge: 0))).ReadAsync<CategoryDto>();
        var (participant, _) = await factory.RegisterAndLoginAsync();

        var entry = await (await participant.PostAsJsonAsync($"/api/categories/{free.CategoryId}/enrolments", TestData.Entry()))
            .ReadAsync<EnrolmentDto>();

        Assert.Equal("Confirmed", entry.EnrolmentStatus);
    }

    [Fact]
    public async Task Paying_the_entry_fee_confirms_the_entry_once()
    {
        var (participant, _) = await factory.RegisterAndLoginAsync();
        var entry = await (await participant.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry())).ReadAsync<EnrolmentDto>();

        var paid = await participant.PatchAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/payment",
            new { amountPaid = 300m, paymentReference = "PAY-TEST-001" });
        var paidAgain = await participant.PatchAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/payment",
            new { amountPaid = 300m, paymentReference = "PAY-TEST-002" });

        await paid.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var confirmed = await paid.ReadAsync<EnrolmentDto>();
        Assert.Equal("Confirmed", confirmed.EnrolmentStatus);
        Assert.Equal(300m, confirmed.AmountPaid);
        Assert.Equal("PAY-TEST-001", confirmed.PaymentReference);
        await paidAgain.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Paying_the_wrong_amount_returns_400()
    {
        var (participant, _) = await factory.RegisterAndLoginAsync();
        var entry = await (await participant.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry())).ReadAsync<EnrolmentDto>();

        var response = await participant.PatchAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/payment",
            new { amountPaid = 250m, paymentReference = "PAY-SHORT" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("amountPaid", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task A_Participant_can_update_their_entry_details()
    {
        var (participant, _) = await factory.RegisterAndLoginAsync();
        var entry = await (await participant.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry("M"))).ReadAsync<EnrolmentDto>();

        var response = await participant.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}",
            new { tShirtSize = "XL", emergencyContactName = "New Contact", emergencyContactPhone = "0839876543" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var updated = await response.ReadAsync<EnrolmentDto>();
        Assert.Equal("XL", updated.TShirtSize);
        Assert.Equal("New Contact", updated.EmergencyContactName);
        Assert.Equal(entry.RaceNumber, updated.RaceNumber);
    }

    [Fact]
    public async Task Withdrawing_marks_the_entry_cancelled_and_keeps_the_record()
    {
        var (participant, _) = await factory.RegisterAndLoginAsync();
        var entry = await (await participant.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry())).ReadAsync<EnrolmentDto>();
        var before = await factory.CreateAnonymousClient().GetFromJsonAsync<CategoryDto>("/api/categories/4");

        var withdraw = await participant.DeleteAsync($"/api/enrolments/{entry.EnrolmentId}");
        var again = await participant.DeleteAsync($"/api/enrolments/{entry.EnrolmentId}");

        await withdraw.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        await again.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        var after = await participant.GetFromJsonAsync<EnrolmentDto>($"/api/enrolments/{entry.EnrolmentId}");
        Assert.Equal("Cancelled", after!.EnrolmentStatus);
        Assert.True(await factory.WithDbAsync(db => db.Enrolments.AnyAsync(e => e.EnrolmentId == entry.EnrolmentId)));
        var category = await factory.CreateAnonymousClient().GetFromJsonAsync<CategoryDto>("/api/categories/4");
        Assert.Equal(before!.EnteredCount - 1, category!.EnteredCount);
    }

    [Fact]
    public async Task Entering_again_after_withdrawing_reinstates_the_same_race_number()
    {
        var (participant, _) = await factory.RegisterAndLoginAsync();
        var entry = await (await participant.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry())).ReadAsync<EnrolmentDto>();
        await participant.DeleteAsync($"/api/enrolments/{entry.EnrolmentId}");

        var response = await participant.PostAsJsonAsync("/api/categories/4/enrolments", TestData.Entry());

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var again = await response.ReadAsync<EnrolmentDto>();
        Assert.Equal(entry.EnrolmentId, again.EnrolmentId);
        Assert.Equal(entry.RaceNumber, again.RaceNumber);
        Assert.Equal("Pending", again.EnrolmentStatus);
    }

    [Fact]
    public async Task Mine_lists_only_the_callers_own_entries()
    {
        var sipho = await factory.CreateClientForAsync(TestUsers.Sipho);

        var all = await sipho.GetFromJsonAsync<PagedResult<EnrolmentDto>>("/api/enrolments/mine");
        var upcoming = await sipho.GetFromJsonAsync<PagedResult<EnrolmentDto>>("/api/enrolments/mine?upcoming=true");
        var past = await sipho.GetFromJsonAsync<PagedResult<EnrolmentDto>>("/api/enrolments/mine?upcoming=false");

        Assert.Equal([7, 1], all!.Items.Select(e => e.EnrolmentId));
        Assert.All(all.Items, e => Assert.Equal(3, e.ParticipantId));
        Assert.Equal("SM0450", Assert.Single(upcoming!.Items).RaceNumber);
        Assert.Equal("CM1042", Assert.Single(past!.Items).RaceNumber);
    }

    [Fact]
    public async Task An_entry_for_an_event_that_has_been_run_can_no_longer_be_changed()
    {
        var sipho = await factory.CreateClientForAsync(TestUsers.Sipho);

        var withdraw = await sipho.DeleteAsync("/api/enrolments/1");
        var update = await sipho.PutAsJsonAsync("/api/enrolments/1", TestData.Entry("L"));

        await withdraw.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        await update.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_event_start_list_can_be_filtered_by_status_and_category()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var pending = await pieter.GetFromJsonAsync<PagedResult<EnrolmentDto>>("/api/events/2/enrolments?status=pending&pageSize=100");
        var walk = await pieter.GetFromJsonAsync<PagedResult<EnrolmentDto>>("/api/events/2/enrolments?categoryId=5&pageSize=100");
        var otherEvent = await pieter.GetAsync("/api/events/2/enrolments?categoryId=1");

        Assert.Contains(pending!.Items, e => e.RaceNumber == "SW2001");
        Assert.All(pending.Items, e => Assert.Equal("Pending", e.EnrolmentStatus));
        Assert.All(walk!.Items, e => Assert.Equal(5, e.CategoryId));
        await otherEvent.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
    }
}
