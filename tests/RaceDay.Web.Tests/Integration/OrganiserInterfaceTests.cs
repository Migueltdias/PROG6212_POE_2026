using System.Net;
using System.Text.RegularExpressions;
using RaceDay.Web.Tests.Infrastructure;

namespace RaceDay.Web.Tests.Integration;

public class OrganiserInterfaceTests(RaceDayWebFactory factory) : IClassFixture<RaceDayWebFactory>
{
    private static (string, string)[] NewEvent(string name, string registrationCloses = "2027-04-20") =>
    [
        ("EventName", name), ("Description", "A flat coastal race."), ("EventDate", "2027-05-09"), ("StartTime", "06:30"),
        ("RegistrationOpens", "2026-09-15"), ("RegistrationCloses", registrationCloses), ("Province", "KwaZulu-Natal"),
        ("City", "Durban"), ("VenueName", "Suncoast Promenade"), ("Latitude", "-29.8395"), ("Longitude", "31.0352"),
        ("FirstCategory.CategoryName", "10km Run"), ("FirstCategory.Discipline", "Run"), ("FirstCategory.DistanceKm", "10.5"),
        ("FirstCategory.EntryFee", "150"), ("FirstCategory.MaxParticipants", "500"), ("FirstCategory.StartTime", "06:30"),
        ("FirstCategory.MinimumAge", "12")
    ];

    private static int EventIdFrom(HttpResponseMessage response) =>
        int.Parse(Regex.Match(response.RedirectPath(), @"/Organiser/Events/Details/(\d+)").Groups[1].Value);

    [Fact]
    public async Task The_dashboard_summarises_the_organisers_events_and_entries()
    {
        var session = await factory.SignedInAsync(Accounts.Thandeka);

        var page = await session.GetPageAsync("/Organiser/Dashboard");

        Assert.Contains("Comrades Marathon 2026", page);
        Assert.Contains("Cape Town Cycle Tour 2027", page);
        Assert.Contains("Upcoming event dates", page);
        Assert.Contains("6 confirmed", page);
        Assert.DoesNotContain("Soweto Marathon 2026", page);
    }

    [Fact]
    public async Task Creating_an_event_with_a_banner_sends_the_image_through_the_api_to_blob_storage()
    {
        var session = await factory.SignedInAsync(Accounts.Thandeka);
        var name = $"Durban Beachfront 10km {Guid.NewGuid():N}"[..40];

        var response = await session.PostMultipartAsync("/Organiser/Events/Create", "/Organiser/Events/Create",
            WebSession.Fields(NewEvent(name)), "Banner", Accounts.Png, "banner.png", "image/png");
        var eventId = EventIdFrom(response);
        var details = await session.GetPageAsync($"/Organiser/Events/Details/{eventId}");
        var storedUrl = await factory.Api.EventBannerAsync(eventId);

        Assert.NotNull(storedUrl);
        Assert.StartsWith($"{ApiHostFactory.FakeImageStorage.BaseUrl}/event-banners/events/{eventId}/", storedUrl);
        Assert.Contains(storedUrl, factory.Api.Images.StoredUrls);
        Assert.Contains($"src=\"{storedUrl}?sig=test\"", details);
        Assert.Contains("was created as a draft", details);
        Assert.Contains("10.5 km", details);
    }

    [Fact]
    public async Task The_apis_validation_messages_appear_on_the_event_form()
    {
        var session = await factory.SignedInAsync(Accounts.Thandeka);

        var response = await session.PostFormAsync("/Organiser/Events/Create", "/Organiser/Events/Create",
            WebSession.Fields(NewEvent("Too Late To Close", registrationCloses: "2027-06-01")));
        var page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The event cannot take place before registration closes.", page);
    }

    [Fact]
    public async Task An_event_can_be_published_and_then_deleted_while_it_has_no_confirmed_entries()
    {
        var session = await factory.SignedInAsync(Accounts.Thandeka);
        var created = await session.PostFormAsync("/Organiser/Events/Create", "/Organiser/Events/Create",
            WebSession.Fields(NewEvent($"Publish Me {Guid.NewGuid():N}"[..30])));
        var eventId = EventIdFrom(created);

        var publish = await session.PostFormAsync($"/Organiser/Events/Details/{eventId}", $"/Organiser/Events/Status/{eventId}",
            WebSession.Fields(("status", "Published")));
        var published = await session.GetPageAsync($"/Organiser/Events/Details/{eventId}");
        var delete = await session.PostFormAsync($"/Organiser/Events/Delete/{eventId}", $"/Organiser/Events/Delete/{eventId}", []);
        var gone = await session.GetAsync($"/Organiser/Events/Details/{eventId}");

        publish.ShouldRedirectTo($"/Organiser/Events/Details/{eventId}");
        Assert.Contains("is live", published);
        delete.ShouldRedirectTo("/Organiser/Events");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }

    [Fact]
    public async Task Deleting_an_event_with_confirmed_entries_explains_why_it_was_refused()
    {
        var session = await factory.SignedInAsync(Accounts.Thandeka);

        var delete = await session.PostFormAsync("/Organiser/Events/Delete/3", "/Organiser/Events/Delete/3", []);
        var details = await session.GetPageAsync("/Organiser/Events/Details/3");

        delete.ShouldRedirectTo("/Organiser/Events/Details/3");
        Assert.Contains("confirmed entries, so it cannot be deleted", details);
    }

    [Fact]
    public async Task An_organiser_can_add_a_category_and_duplicate_names_are_refused()
    {
        var session = await factory.SignedInAsync(Accounts.Thandeka);
        (string, string)[] category =
        [
            ("NewCategory.CategoryName", "Under 16 5km Ride"), ("NewCategory.Discipline", "Cycle"), ("NewCategory.DistanceKm", "5"),
            ("NewCategory.EntryFee", "0"), ("NewCategory.MaxParticipants", "300"), ("NewCategory.StartTime", "12:00"),
            ("NewCategory.MinimumAge", "8")
        ];

        var added = await session.PostFormAsync("/Organiser/Categories?eventId=3", "/Organiser/Categories/Create?eventId=3", WebSession.Fields(category));
        var list = await session.GetPageAsync("/Organiser/Categories?eventId=3");
        var duplicate = await session.PostFormAsync("/Organiser/Categories?eventId=3", "/Organiser/Categories/Create?eventId=3", WebSession.Fields(category));

        added.ShouldRedirectTo("/Organiser/Categories?eventId=3");
        Assert.Contains("Under 16 5km Ride", list);
        Assert.Contains("already has a category called", await duplicate.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task The_enrolments_page_lists_participants_with_colour_coded_statuses()
    {
        var session = await factory.SignedInAsync(Accounts.Pieter);

        var page = await session.GetPageAsync("/Organiser/Enrolments?eventId=2");
        var pendingOnly = await session.GetPageAsync("/Organiser/Enrolments?eventId=2&status=Pending");

        Assert.Contains("SM0450", page);
        Assert.Contains("Lerato Dlamini", page);
        Assert.Contains("21.1km Half Marathon", page);
        Assert.Contains("rd-status rd-status--confirmed", page);
        Assert.Contains("rd-status rd-status--pending", page);
        Assert.Contains("SW2001", pendingOnly);
        Assert.DoesNotContain("SM0450", pendingOnly);
    }

    [Fact]
    public async Task Another_organisers_event_shows_a_friendly_403_page()
    {
        var session = await factory.SignedInAsync(Accounts.Thandeka);

        var response = await session.GetAsync("/Organiser/Events/Details/2");
        var page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("Only the Organiser who created this event can manage it.", page);
    }

    [Fact]
    public async Task The_results_page_lists_confirmed_entrants_and_their_placings()
    {
        var session = await factory.SignedInAsync(Accounts.Thandeka);

        var overview = await session.GetPageAsync("/Organiser/Results?eventId=1");
        var capture = await session.GetPageAsync("/Organiser/Results/Capture?categoryId=1");

        Assert.Contains("Results are published", overview);
        Assert.Contains("Sipho Ndlovu", capture);
        Assert.Contains("rd-medal rd-medal--1", capture);
        Assert.Contains("5:42:47", capture);
    }

    [Fact]
    public async Task Results_cannot_be_captured_before_race_day()
    {
        var session = await factory.SignedInAsync(Accounts.Pieter);

        var page = await session.GetPageAsync("/Organiser/Results/Capture?categoryId=3");

        Assert.Contains("has not been run yet", page);
        Assert.DoesNotContain("name=\"rows[0].ResultStatus\"", page);
    }
}
