using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class CategoryAndRouteTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task An_Organiser_adds_an_age_category_to_their_event()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync("/api/events/3/categories",
            TestData.NewCategory("Under 20 21km Ride", "Cycle", 21m, entryFee: 120m, minimumAge: 14));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var category = await response.ReadAsync<CategoryDto>();
        Assert.Equal("Cycle", category.Discipline);
        Assert.Equal(14, category.MinimumAge);
        var list = await factory.CreateAnonymousClient().GetFromJsonAsync<List<CategoryDto>>("/api/events/3/categories");
        Assert.Contains(list!, c => c.CategoryId == category.CategoryId);
    }

    [Fact]
    public async Task Category_names_are_unique_within_an_event()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync("/api/events/3/categories", TestData.NewCategory("109KM RACE TOUR", "Cycle", 109m));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Invalid_category_values_return_400()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync("/api/events/3/categories",
            TestData.NewCategory("Swim", "Swim", 0m, entryFee: -5m, maxParticipants: 0, minimumAge: 150));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var errors = (await response.ReadValidationProblemAsync()).Errors.Keys;
        Assert.Contains("discipline", errors);
        Assert.Contains("distanceKm", errors);
        Assert.Contains("entryFee", errors);
        Assert.Contains("maxParticipants", errors);
        Assert.Contains("minimumAge", errors);
    }

    [Fact]
    public async Task Another_Organiser_cannot_add_categories_to_the_event()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var response = await pieter.PostAsJsonAsync("/api/events/3/categories", TestData.NewCategory("Gate Crasher"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task The_limit_cannot_be_lowered_below_the_number_already_entered()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var response = await pieter.PutAsJsonAsync("/api/categories/3",
            TestData.NewCategory("42.2km Marathon", "Run", 42.2m, 450m, maxParticipants: 1, minimumAge: 18));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_owner_can_update_a_category()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);
        var ev = await TestData.CreateEventAsync(pieter, publish: false, TestData.NewCategory("Fun Run"));
        var categoryId = ev.Categories.Single().CategoryId;

        var response = await pieter.PutAsJsonAsync($"/api/categories/{categoryId}",
            TestData.NewCategory("Fun Run 8km", "Run", 8m, entryFee: 90m, maxParticipants: 300));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var updated = await response.ReadAsync<CategoryDto>();
        Assert.Equal("Fun Run 8km", updated.CategoryName);
        Assert.Equal(90m, updated.EntryFee);
    }

    [Fact]
    public async Task A_category_with_entries_cannot_be_removed()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var response = await pieter.DeleteAsync("/api/categories/3");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task The_owner_can_remove_an_empty_category()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);
        var created = await pieter.PostAsJsonAsync("/api/events/2/categories", TestData.NewCategory($"Spare {Guid.NewGuid():N}"));
        var category = await created.ReadAsync<CategoryDto>();

        var response = await pieter.DeleteAsync($"/api/categories/{category.CategoryId}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        await (await pieter.GetAsync($"/api/categories/{category.CategoryId}")).ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task The_last_category_of_a_published_event_cannot_be_removed()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);
        var ev = await TestData.CreateEventAsync(pieter, publish: true, TestData.NewCategory("Only Distance"));

        var response = await pieter.DeleteAsync($"/api/categories/{ev.Categories.Single().CategoryId}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_category_reports_how_many_places_are_left()
    {
        var category = await factory.CreateAnonymousClient().GetFromJsonAsync<CategoryDto>("/api/categories/3");

        Assert.Equal(8000, category!.MaxParticipants);
        Assert.Equal(2, category.EnteredCount);
        Assert.Equal(7998, category.PlacesLeft);
    }

    [Fact]
    public async Task Categories_of_a_draft_event_are_hidden_from_the_public()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);
        var draft = await TestData.CreateEventAsync(thandeka, publish: false, TestData.NewCategory());
        var categoryId = draft.Categories.Single().CategoryId;

        await (await factory.CreateAnonymousClient().GetAsync($"/api/categories/{categoryId}")).ShouldHaveStatusAsync(HttpStatusCode.NotFound);
        await (await thandeka.GetAsync($"/api/categories/{categoryId}")).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task A_route_is_returned_in_sequence_order()
    {
        var route = await factory.CreateAnonymousClient().GetFromJsonAsync<List<WaypointDto>>("/api/categories/1/waypoints")
                    ?? throw new InvalidOperationException("No route was returned.");

        Assert.Equal(Enumerable.Range(1, 9), route.Select(w => w.SequenceNumber));
        Assert.Equal("Start", route[0].WaypointType);
        Assert.Equal("Finish", route[^1].WaypointType);
        Assert.Equal("Polly Shortts", route[7].WaypointName);
    }

    [Fact]
    public async Task The_owner_can_add_update_and_remove_a_waypoint()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var created = await thandeka.PostAsJsonAsync("/api/categories/7/waypoints", TestData.NewWaypoint(1, 0m, "Start"));
        await created.ShouldHaveStatusAsync(HttpStatusCode.Created);
        var waypoint = await created.ReadAsync<WaypointDto>();

        var updated = await thandeka.PutAsJsonAsync($"/api/waypoints/{waypoint.WaypointId}", TestData.NewWaypoint(2, 5m));
        await updated.ShouldHaveStatusAsync(HttpStatusCode.OK);
        Assert.Equal(2, (await updated.ReadAsync<WaypointDto>()).SequenceNumber);

        await (await thandeka.DeleteAsync($"/api/waypoints/{waypoint.WaypointId}")).ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        var route = await thandeka.GetFromJsonAsync<List<WaypointDto>>("/api/categories/7/waypoints");
        Assert.DoesNotContain(route!, w => w.WaypointId == waypoint.WaypointId);
    }

    [Fact]
    public async Task A_sequence_number_can_only_be_used_once_per_route()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync("/api/categories/1/waypoints", TestData.NewWaypoint(5, 40m));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task A_waypoint_cannot_be_further_from_the_start_than_the_route_is_long()
    {
        var thandeka = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var response = await thandeka.PostAsJsonAsync("/api/categories/8/waypoints", TestData.NewWaypoint(1, 12m));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("distanceFromStartKm", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task Another_Organiser_cannot_change_the_route()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var response = await pieter.PutAsJsonAsync("/api/waypoints/10", TestData.NewWaypoint(1, 0m, "Start"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }
}
