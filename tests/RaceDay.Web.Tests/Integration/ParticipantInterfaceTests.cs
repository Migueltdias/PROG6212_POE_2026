using System.Net;
using System.Text.RegularExpressions;
using RaceDay.Web.Tests.Infrastructure;

namespace RaceDay.Web.Tests.Integration;

public class ParticipantInterfaceTests(RaceDayWebFactory factory) : IClassFixture<RaceDayWebFactory>
{
    private static (string, string)[] Entry(int eventId, int categoryId) =>
    [
        ("EventId", eventId.ToString()), ("CategoryId", categoryId.ToString()), ("TShirtSize", "S"),
        ("EmergencyContactName", "Zanele Khumalo"), ("EmergencyContactPhone", "0713214567")
    ];

    [Fact]
    public async Task The_home_page_lists_upcoming_events_only()
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        var page = await session.GetPageAsync("/Participant/Events");

        Assert.Contains("Soweto Marathon 2026", page);
        Assert.Contains("Cape Town Cycle Tour 2027", page);
        Assert.DoesNotContain("Comrades Marathon 2026", page);
    }

    [Theory]
    [InlineData("Discipline=Cycle", "Cape Town Cycle Tour 2027", "Soweto Marathon 2026")]
    [InlineData("Province=Gauteng", "Soweto Marathon 2026", "Cape Town Cycle Tour 2027")]
    [InlineData("Search=peninsula", "Cape Town Cycle Tour 2027", "Soweto Marathon 2026")]
    [InlineData("ToDate=2026-12-31", "Soweto Marathon 2026", "Cape Town Cycle Tour 2027")]
    public async Task The_home_page_filters_events(string filter, string shown, string hidden)
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        var page = await session.GetPageAsync($"/Participant/Events?{filter}");

        Assert.Contains(shown, page);
        Assert.DoesNotContain(hidden, page);
    }

    [Fact]
    public async Task Event_details_show_every_category_with_an_enter_button()
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        var page = await session.GetPageAsync("/Participant/Events/Details/2");

        Assert.Contains("42.2km Marathon", page);
        Assert.Contains("21.1km Half Marathon", page);
        Assert.Contains("10km Race Walk", page);
        Assert.Contains("Enrolments/Create?eventId=2&amp;categoryId=4", page);
        Assert.Contains("Pieter van Wyk", page);
    }

    [Fact]
    public async Task Entering_records_the_enrolment_at_once_and_paying_confirms_it()
    {
        var session = await factory.SignedInAsync(Accounts.Nomsa);

        var entered = await session.PostFormAsync("/Participant/Enrolments/Create?eventId=2&categoryId=4",
            "/Participant/Enrolments/Create", WebSession.Fields(Entry(2, 4)));
        var statusAfterEntering = await factory.Api.EnrolmentStatusAsync(participantId: 7, categoryId: 4);
        var payUrl = entered.RedirectPath();
        var paid = await session.PostFormAsync(payUrl, payUrl, []);
        var statusAfterPaying = await factory.Api.EnrolmentStatusAsync(participantId: 7, categoryId: 4);
        var myEvents = await session.GetPageAsync("/Participant/Enrolments");

        Assert.Matches(@"^/Participant/Enrolments/Pay/\d+$", payUrl);
        Assert.Equal("Pending", statusAfterEntering);
        paid.ShouldRedirectTo("/Participant/Enrolments");
        Assert.Equal("Confirmed", statusAfterPaying);
        Assert.Contains("Payment of R300.00 received", myEvents);
        Assert.Matches(new Regex(@"rd-bib"">SM\d{4}<"), myEvents);
    }

    [Fact]
    public async Task Entering_the_same_category_twice_shows_the_apis_refusal()
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        var response = await session.PostFormAsync("/Participant/Enrolments/Create?eventId=3&categoryId=6",
            "/Participant/Enrolments/Create", WebSession.Fields(Entry(3, 6)));
        var page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("You have already entered this category (race number CT0911).", page);
    }

    [Fact]
    public async Task My_events_shows_each_entry_with_its_race_number_and_status()
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        var page = await session.GetPageAsync("/Participant/Enrolments?tab=all");

        Assert.Contains("CT0911", page);
        Assert.Contains("CC0071", page);
        Assert.Contains("Cape Town Cycle Tour 2027", page);
        Assert.Contains("rd-status rd-status--confirmed", page);
    }

    [Fact]
    public async Task My_results_show_the_finish_time_and_position_out_of_the_field()
    {
        var ayesha = await factory.SignedInAsync(Accounts.Ayesha);
        var sipho = await factory.SignedInAsync(Accounts.Sipho);

        var ayeshasResults = await ayesha.GetPageAsync("/Participant/Results");
        var siphosResults = await sipho.GetPageAsync("/Participant/Results");

        Assert.Contains("Comrades Community 10km", ayeshasResults);
        Assert.Contains("48:12", ayeshasResults);
        Assert.Contains("1st out of 2 finishers", ayeshasResults);
        Assert.Contains("1st out of 3 finishers", siphosResults);
        Assert.Contains("5:42:47", siphosResults);
    }

    [Fact]
    public async Task A_profile_picture_is_uploaded_through_the_api_and_shown_in_the_navbar()
    {
        var session = await factory.SignedInAsync(Accounts.Sipho);

        var upload = await session.PostMultipartAsync("/Profile", "/Profile/Picture", [], "picture", Accounts.Png, "me.png", "image/png");
        var profile = await session.GetPageAsync("/Profile");

        upload.ShouldRedirectTo("/Profile");
        var pictureUrl = Assert.Single(factory.Api.Images.StoredUrls, u => u.Contains("/profile-pictures/users/3/"));
        Assert.Contains($"src=\"{pictureUrl}?sig=test\"", profile);
        Assert.Contains("Your new profile picture is saved", profile);
    }

    [Fact]
    public async Task Updating_the_profile_refreshes_the_name_in_the_session()
    {
        var session = await factory.SignedInAsync(Accounts.Nomsa);

        var update = await session.PostFormAsync("/Profile", "/Profile/Update", WebSession.Fields(
            ("Details.FirstName", "Nomsa"), ("Details.LastName", "Khumalo-Dube"), ("Details.PhoneNumber", "0713698520"),
            ("Details.DateOfBirth", "2000-01-19"), ("Details.Gender", "F")));
        var page = await session.GetPageAsync("/Profile");

        update.ShouldRedirectTo("/Profile");
        Assert.Contains("Nomsa Khumalo-Dube", page);
    }

    [Fact]
    public async Task A_wrong_current_password_is_reported_on_the_password_field()
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        var response = await session.PostFormAsync("/Profile", "/Profile/Password", WebSession.Fields(
            ("Password.CurrentPassword", "Not@MyPassword1"), ("Password.NewPassword", "NewRace@2027"), ("Password.ConfirmNewPassword", "NewRace@2027")));
        var page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("The current password is incorrect.", page);
    }
}
