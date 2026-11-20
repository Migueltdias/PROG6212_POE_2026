extern alias ApiHost;

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Web.Tests.Infrastructure;
using ISessionStore = ApiHost::RaceDay.Api.Security.ISessionStore;

namespace RaceDay.Web.Tests.Integration;

public class ErrorHandlingTests(RaceDayWebFactory factory) : IClassFixture<RaceDayWebFactory>
{
    [Fact]
    public async Task An_unknown_page_shows_a_friendly_404()
    {
        var response = await factory.NewSession().GetAsync("/no/such/page");
        var page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("We couldn&#x27;t find that", page);
    }

    [Fact]
    public async Task A_missing_record_shows_a_friendly_404()
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        var response = await session.GetAsync("/Participant/Events/Details/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("rd-empty", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task When_the_api_is_down_signed_in_pages_show_a_try_again_page_without_raw_errors()
    {
        var session = await factory.SignedInAsync(Accounts.Ayesha);

        factory.ApiDown = true;
        HttpResponseMessage response;
        try
        {
            response = await session.GetAsync("/Participant/Events");
        }
        finally
        {
            factory.ApiDown = false;
        }

        var page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("RaceDay is catching its breath", page);
        Assert.DoesNotContain("Exception", page);
        Assert.DoesNotContain("refused", page);
    }

    [Fact]
    public async Task When_the_api_is_down_login_and_the_landing_page_still_respond_gracefully()
    {
        var session = factory.NewSession();
        await session.GetPageAsync("/Account/Login");

        factory.ApiDown = true;
        HttpResponseMessage login;
        HttpResponseMessage landing;
        try
        {
            login = await session.LoginAsync(Accounts.Ayesha);
            landing = await session.GetAsync("/");
        }
        finally
        {
            factory.ApiDown = false;
        }

        Assert.Equal(HttpStatusCode.ServiceUnavailable, login.StatusCode);
        Assert.Contains("catching its breath", await login.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, landing.StatusCode);
        Assert.Contains("Events are taking a breather", await landing.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task A_session_the_api_has_ended_sends_the_user_back_to_login()
    {
        var session = await factory.SignedInAsync(Accounts.Sipho);
        factory.Api.Services.GetRequiredService<ISessionStore>().RevokeAllForUser(3);

        var response = await session.GetAsync("/Participant/Enrolments");
        var login = await session.GetPageAsync("/Account/Login");

        response.ShouldRedirectTo("/Account/Login?returnUrl=%2FParticipant%2FEnrolments");
        Assert.Contains("Your session has ended", login);
        Assert.Equal(HttpStatusCode.Redirect, (await session.GetAsync("/Participant/Results")).StatusCode);
    }

    [Fact]
    public async Task Invalid_form_input_is_redisplayed_with_messages_instead_of_failing()
    {
        var session = await factory.SignedInAsync(Accounts.Thandeka);

        var response = await session.PostFormAsync("/Organiser/Events/Create", "/Organiser/Events/Create", WebSession.Fields(
            ("EventName", ""), ("EventDate", "not-a-date"), ("Latitude", "200")));
        var page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Give the event a name.", page);
        Assert.Contains("Latitude is between -90 and 90.", page);
    }
}
