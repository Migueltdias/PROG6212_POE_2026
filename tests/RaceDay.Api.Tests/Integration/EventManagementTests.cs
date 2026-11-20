using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class EventManagementTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task An_Organiser_creates_an_event_as_a_draft_with_its_distances_and_types()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync("/api/events", TestData.NewEvent(categories:
        [
            TestData.NewCategory("10km Run", "Run", 10m),
            TestData.NewCategory("5km Walk", "walk", 5m, entryFee: 0m)
        ]));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var ev = await response.ReadAsync<EventDetailDto>();
        Assert.Equal("Draft", ev.EventStatus);
        Assert.Equal(1, ev.OrganiserId);
        Assert.Equal(["Run", "Walk"], ev.Disciplines);
        Assert.Equal(5m, ev.ShortestDistanceKm);
        Assert.Equal(10m, ev.LongestDistanceKm);
        Assert.Equal(["10km Run", "5km Walk"], ev.Categories.Select(c => c.CategoryName));
        Assert.Equal($"/api/events/{ev.EventId}", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task A_draft_is_only_visible_to_the_Organiser_who_created_it()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var draft = await TestData.CreateEventAsync(thandeka, publish: false, TestData.NewCategory());

        var asOwner = await thandeka.GetAsync($"/api/events/{draft.EventId}");
        var asAnonymous = await factory.CreateAnonymousClient().GetAsync($"/api/events/{draft.EventId}");
        var asOtherOrganiser = await (await factory.CreateClientForAsync(TestUsers.Pieter)).GetAsync($"/api/events/{draft.EventId}");
        var browse = await factory.CreateAnonymousClient().GetFromJsonAsync<PagedResult<EventSummaryDto>>("/api/events?pageSize=100");

        await asOwner.ShouldHaveStatusAsync(HttpStatusCode.OK);
        await asAnonymous.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        await asOtherOrganiser.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        Assert.DoesNotContain(browse!.Items, e => e.EventId == draft.EventId);
    }

    [Fact]
    public async Task Creating_an_event_dated_in_the_past_returns_400()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync("/api/events",
            TestData.NewEvent(eventDate: "2026-05-01", registrationOpens: "2026-01-01", registrationCloses: "2026-04-30"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("eventDate", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task Creating_an_event_that_runs_before_registration_closes_returns_400()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync("/api/events",
            TestData.NewEvent(eventDate: "2027-01-01", registrationCloses: "2027-04-30"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("eventDate", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task Creating_an_event_in_an_unknown_province_returns_400()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync("/api/events", TestData.NewEvent(province: "Atlantis"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("province", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task A_second_event_with_the_same_name_on_the_same_date_returns_409()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var name = $"Duplicate Check {Guid.NewGuid():N}";
        await (await thandeka.PostAsJsonAsync("/api/events", TestData.NewEvent(name))).ShouldHaveStatusAsync(HttpStatusCode.Created);

        var response = await thandeka.PostAsJsonAsync("/api/events", TestData.NewEvent(name.ToUpperInvariant()));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_owner_can_update_their_event()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var ev = await TestData.CreateEventAsync(thandeka, publish: false);
        var newName = $"Renamed Event {Guid.NewGuid():N}";

        var response = await thandeka.PutAsJsonAsync($"/api/events/{ev.EventId}", TestData.NewEvent(newName));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        Assert.Equal(newName, (await response.ReadAsync<EventDetailDto>()).EventName);
    }

    [Fact]
    public async Task Another_Organiser_cannot_update_or_delete_the_event()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);
        var ev = await TestData.CreateEventAsync(thandeka, publish: false);

        var update = await pieter.PutAsJsonAsync($"/api/events/{ev.EventId}", TestData.NewEvent("Hijacked"));
        var delete = await pieter.DeleteAsync($"/api/events/{ev.EventId}");

        await update.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
        await delete.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
        await (await thandeka.GetAsync($"/api/events/{ev.EventId}")).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Updating_an_event_that_does_not_exist_returns_404()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PutAsJsonAsync("/api/events/9999", TestData.NewEvent());

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_event_without_categories_cannot_be_published()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var ev = await TestData.CreateEventAsync(thandeka, publish: false);

        var response = await thandeka.PatchAsJsonAsync($"/api/events/{ev.EventId}/status", new { status = "Published" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        Assert.Contains("no categories", (await response.ReadProblemAsync()).Detail);
    }

    [Fact]
    public async Task Publishing_makes_the_event_visible_to_everyone()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var ev = await TestData.CreateEventAsync(thandeka, publish: true, TestData.NewCategory());

        Assert.Equal("Published", ev.EventStatus);
        var anonymous = factory.CreateAnonymousClient();
        await (await anonymous.GetAsync($"/api/events/{ev.EventId}")).ShouldHaveStatusAsync(HttpStatusCode.OK);
        var browse = await anonymous.GetFromJsonAsync<PagedResult<EventSummaryDto>>("/api/events?pageSize=100");
        Assert.Contains(browse!.Items, e => e.EventId == ev.EventId);
    }

    [Fact]
    public async Task A_completed_event_cannot_change_status_again()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PatchAsJsonAsync("/api/events/1/status", new { status = "Published" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_event_cannot_be_completed_before_its_date()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var ev = await TestData.CreateEventAsync(thandeka, publish: true, TestData.NewCategory());

        var response = await thandeka.PatchAsJsonAsync($"/api/events/{ev.EventId}/status", new { status = "Completed" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task An_unknown_status_returns_400()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PatchAsJsonAsync("/api/events/3/status", new { status = "Postponed" });

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task The_owner_can_delete_an_event_without_confirmed_entries()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var ev = await TestData.CreateEventAsync(thandeka, publish: false, TestData.NewCategory());

        var response = await thandeka.DeleteAsync($"/api/events/{ev.EventId}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        await (await thandeka.GetAsync($"/api/events/{ev.EventId}")).ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_event_with_confirmed_entries_cannot_be_deleted()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var response = await pieter.DeleteAsync("/api/events/2");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        await (await pieter.GetAsync("/api/events/2")).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Mine_lists_only_the_callers_events_including_drafts()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);
        var draft = await TestData.CreateEventAsync(pieter, publish: false);

        var all = await pieter.GetFromJsonAsync<PagedResult<EventSummaryDto>>("/api/events/mine?pageSize=100");
        var drafts = await pieter.GetFromJsonAsync<PagedResult<EventSummaryDto>>("/api/events/mine?status=draft&pageSize=100");

        Assert.All(all!.Items, e => Assert.Equal(2, e.OrganiserId));
        Assert.Contains(all.Items, e => e.EventId == 2);
        Assert.Contains(all.Items, e => e.EventId == draft.EventId);
        Assert.All(drafts!.Items, e => Assert.Equal("Draft", e.EventStatus));
    }

    [Fact]
    public async Task Both_roles_and_visitors_can_browse_events_soonest_first()
    {
        var participant = await factory.CreateClientForAsync(TestUsers.Ayesha);

        var page = await participant.GetFromJsonAsync<PagedResult<EventSummaryDto>>("/api/events?pageSize=100");

        Assert.NotEmpty(page!.Items);
        Assert.DoesNotContain(page.Items, e => e.EventStatus == "Draft");
        Assert.Equal(page.Items.OrderBy(e => e.EventDate).Select(e => e.EventId), page.Items.Select(e => e.EventId));
        Assert.Equal("Comrades Marathon 2026", page.Items[0].EventName);
    }

    [Theory]
    [InlineData("discipline=cycle", "Cape Town Cycle Tour 2027")]
    [InlineData("province=gauteng", "Soweto Marathon 2026")]
    [InlineData("search=SOWETO", "Soweto Marathon 2026")]
    [InlineData("fromDate=2027-01-01&toDate=2027-12-31&search=cycle", "Cape Town Cycle Tour 2027")]
    public async Task Browsing_can_be_filtered(string query, string expectedEvent)
    {
        var page = await factory.CreateAnonymousClient().GetFromJsonAsync<PagedResult<EventSummaryDto>>($"/api/events?{query}");

        Assert.Equal(expectedEvent, Assert.Single(page!.Items).EventName);
    }

    [Fact]
    public async Task Browsing_with_toDate_before_fromDate_returns_400()
    {
        var response = await factory.CreateAnonymousClient().GetAsync("/api/events?fromDate=2027-01-01&toDate=2026-01-01");

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Event_detail_lists_each_category_with_places_left()
    {
        var ev = await factory.CreateAnonymousClient().GetFromJsonAsync<EventDetailDto>("/api/events/2");

        Assert.Equal(["42.2km Marathon", "21.1km Half Marathon", "10km Race Walk"], ev!.Categories.Select(c => c.CategoryName));
        var marathon = ev.Categories[0];
        Assert.Equal(2, marathon.EnteredCount);
        Assert.Equal(7998, marathon.PlacesLeft);
        Assert.Equal(["Run", "Walk"], ev.Disciplines);
    }
}
