using System.Net;
using RaceDay.Web.Tests.Infrastructure;

namespace RaceDay.Web.Tests.Integration;

public class AuthenticationTests(RaceDayWebFactory factory) : IClassFixture<RaceDayWebFactory>
{
    [Theory]
    [InlineData("/Organiser/Dashboard")]
    [InlineData("/Organiser/Events/Create")]
    [InlineData("/Participant/Events")]
    [InlineData("/Participant/Enrolments")]
    [InlineData("/Participant/Results")]
    [InlineData("/Profile")]
    public async Task Anonymous_visitors_are_redirected_to_login_from_protected_pages(string url)
    {
        var response = await factory.NewSession().GetAsync(url);

        response.ShouldRedirectTo($"/Account/Login?returnUrl={Uri.EscapeDataString(url)}");
    }

    [Fact]
    public async Task An_organiser_signs_in_to_the_organiser_dashboard_with_organiser_navigation()
    {
        var session = factory.NewSession();

        var login = await session.LoginAsync(Accounts.Thandeka);
        var dashboard = await session.GetPageAsync("/Organiser/Dashboard");

        login.ShouldRedirectTo("/Organiser/Dashboard");
        Assert.Contains("Hi, Thandeka", dashboard);
        Assert.Contains("rd-role-chip--organiser", dashboard);
        Assert.Contains("New event", dashboard);
        Assert.DoesNotContain("My results", dashboard);
    }

    [Fact]
    public async Task A_participant_signs_in_to_upcoming_events_with_participant_navigation()
    {
        var session = factory.NewSession();

        var login = await session.LoginAsync(Accounts.Ayesha);
        var home = await session.GetPageAsync("/Participant/Events");

        login.ShouldRedirectTo("/Participant/Events");
        Assert.Contains("Find your next race", home);
        Assert.Contains("rd-role-chip--participant", home);
        Assert.Contains("My results", home);
        Assert.DoesNotContain("New event", home);
    }

    [Fact]
    public async Task A_wrong_password_shows_the_apis_message_and_starts_no_session()
    {
        var session = factory.NewSession();

        var login = await session.LoginAsync(Accounts.Ayesha, "Wrong@Password1");
        var page = await login.Content.ReadAsStringAsync();
        var profile = await session.GetAsync("/Profile");

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("The email address or password is incorrect.", page);
        Assert.Equal(HttpStatusCode.Redirect, profile.StatusCode);
    }

    [Fact]
    public async Task The_session_remembers_the_user_and_role_across_pages()
    {
        var session = await factory.SignedInAsync(Accounts.Sipho);

        var home = await session.GetAsync("/");
        var profile = await session.GetPageAsync("/Profile");

        home.ShouldRedirectTo("/Participant/Events");
        Assert.Contains(Accounts.Sipho, profile);
        Assert.Contains("Sipho Ndlovu", profile);
    }

    [Fact]
    public async Task A_participant_is_kept_out_of_organiser_pages()
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        var dashboard = await session.GetAsync("/Organiser/Dashboard");
        var denied = await session.GetAsync("/Account/AccessDenied");

        dashboard.ShouldRedirectTo("/Account/AccessDenied");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Contains("participant", await denied.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task An_organiser_is_kept_out_of_participant_pages()
    {
        var session = await factory.SignedInAsync(Accounts.Pieter);

        var response = await session.GetAsync("/Participant/Enrolments");

        response.ShouldRedirectTo("/Account/AccessDenied");
    }

    [Fact]
    public async Task Logging_out_clears_the_session_and_returns_to_login()
    {
        var session = await factory.SignedInAsync(Accounts.Pieter);

        var logout = await session.PostFormAsync("/Organiser/Dashboard", "/Account/Logout", []);
        var afterwards = await session.GetAsync("/Organiser/Dashboard");
        var loginPage = await session.GetPageAsync("/Account/Login");

        logout.ShouldRedirectTo("/Account/Login");
        Assert.Equal(HttpStatusCode.Redirect, afterwards.StatusCode);
        Assert.StartsWith("/Account/Login", afterwards.RedirectPath());
        Assert.Contains("You have signed out", loginPage);
    }

    [Fact]
    public async Task Logout_must_be_a_post_with_an_antiforgery_token()
    {
        var session = await factory.SignedInAsync(Accounts.Pieter);

        var forged = await session.Client.PostAsync("/Account/Logout", new FormUrlEncodedContent([]));
        var stillSignedIn = await session.GetAsync("/Organiser/Dashboard");

        Assert.Equal(HttpStatusCode.BadRequest, forged.StatusCode);
        Assert.Equal(HttpStatusCode.OK, stillSignedIn.StatusCode);
    }

    [Fact]
    public async Task Signing_in_returns_the_user_to_the_page_they_asked_for()
    {
        var session = factory.NewSession();
        await session.GetAsync("/Participant/Results");

        var login = await session.PostFormAsync("/Account/Login?returnUrl=%2FParticipant%2FResults", "/Account/Login",
            WebSession.Fields(("Email", Accounts.Sipho), ("Password", WebSession.Password), ("ReturnUrl", "/Participant/Results")));

        login.ShouldRedirectTo("/Participant/Results");
    }

    [Fact]
    public async Task A_return_url_into_the_other_roles_interface_is_ignored()
    {
        var session = factory.NewSession();

        var login = await session.PostFormAsync("/Account/Login", "/Account/Login",
            WebSession.Fields(("Email", Accounts.Thandeka), ("Password", WebSession.Password), ("ReturnUrl", "/Participant/Results")));

        login.ShouldRedirectTo("/Organiser/Dashboard");
    }

    [Fact]
    public async Task Registering_signs_the_new_user_in_with_the_role_they_chose()
    {
        var session = factory.NewSession();
        var email = $"zanele.{Guid.NewGuid():N}@example.co.za";

        var response = await session.PostFormAsync("/Account/Register", "/Account/Register", WebSession.Fields(
            ("Role", "Organiser"), ("FirstName", "Zanele"), ("LastName", "Mthembu"), ("Email", email),
            ("PhoneNumber", "0821234567"), ("DateOfBirth", "1988-03-04"), ("Gender", "F"),
            ("Password", "Finish@Line1"), ("ConfirmPassword", "Finish@Line1")));
        var dashboard = await session.GetPageAsync("/Organiser/Dashboard");

        response.ShouldRedirectTo("/Organiser/Dashboard");
        Assert.Contains("Hi, Zanele", dashboard);
        Assert.Contains("Welcome to RaceDay, Zanele", dashboard);
    }

    [Fact]
    public async Task The_apis_registration_rules_are_shown_on_the_form()
    {
        var session = factory.NewSession();

        var response = await session.PostFormAsync("/Account/Register", "/Account/Register", WebSession.Fields(
            ("Role", "Organiser"), ("FirstName", "Young"), ("LastName", "Organiser"), ("Email", $"young.{Guid.NewGuid():N}@example.co.za"),
            ("DateOfBirth", "2012-01-01"), ("Gender", "M"), ("Password", "Finish@Line1"), ("ConfirmPassword", "Finish@Line1")));
        var page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Organisers must be at least 18 years old.", page);
    }

    [Fact]
    public async Task Pages_behind_a_login_are_not_cached_by_the_browser()
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        var response = await session.GetAsync("/Participant/Enrolments");

        Assert.Contains("no-store", response.Headers.CacheControl?.ToString());
    }
}
