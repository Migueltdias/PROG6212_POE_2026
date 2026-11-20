using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class UserProfileTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Theory]
    [InlineData(TestUsers.Sipho, 3, "Participant")]
    [InlineData(TestUsers.Thandeka, 1, "Organiser")]
    public async Task Get_me_returns_the_callers_own_profile(string email, int userId, string role)
    {
        var client = await factory.CreateClientForAsync(email);

        var me = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");

        Assert.Equal(userId, me!.UserId);
        Assert.Equal(role, me.Role);
        Assert.Equal(email, me.Email);
    }

    [Fact]
    public async Task Update_me_changes_the_editable_details()
    {
        var (client, profile) = await factory.RegisterAndLoginAsync();

        var response = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Updated",
            lastName = "Name",
            phoneNumber = "0831234567",
            dateOfBirth = "1990-02-02",
            gender = "o"
        });

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var me = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");
        Assert.Equal("Updated", me!.FirstName);
        Assert.Equal("0831234567", me.PhoneNumber);
        Assert.Equal(new DateOnly(1990, 2, 2), me.DateOfBirth);
        Assert.Equal("O", me.Gender);
        Assert.Equal(profile.Email, me.Email);
    }

    [Fact]
    public async Task Update_me_cannot_change_the_role_or_email()
    {
        var (client, profile) = await factory.RegisterAndLoginAsync();

        await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Sneaky",
            lastName = "Runner",
            dateOfBirth = "1995-04-12",
            gender = "F",
            role = "Organiser",
            email = "someone.else@example.co.za"
        });

        var me = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");
        Assert.Equal("Participant", me!.Role);
        Assert.Equal(profile.Email, me.Email);
        await (await client.GetAsync("/api/events/mine")).ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_me_with_a_date_of_birth_in_the_future_returns_400()
    {
        var (client, _) = await factory.RegisterAndLoginAsync();

        var response = await client.PutAsJsonAsync("/api/users/me", new
        {
            firstName = "Future", lastName = "Born", dateOfBirth = "2030-01-01", gender = "M"
        });

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        Assert.Contains("dateOfBirth", (await response.ReadValidationProblemAsync()).Errors.Keys);
    }

    [Fact]
    public async Task Changing_the_password_means_only_the_new_one_works()
    {
        var (client, profile) = await factory.RegisterAndLoginAsync();

        var change = await client.PutAsJsonAsync("/api/users/me/password", new
        {
            currentPassword = TestUsers.Password,
            newPassword = "Changed@2027",
            confirmNewPassword = "Changed@2027"
        });

        await change.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        var anonymous = factory.CreateAnonymousClient();
        var oldPassword = await anonymous.PostAsJsonAsync("/api/auth/login", new { email = profile.Email, password = TestUsers.Password });
        await oldPassword.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        Assert.Equal(profile.UserId, (await anonymous.LoginAsync(profile.Email, "Changed@2027")).User.UserId);
    }

    [Fact]
    public async Task Changing_the_password_with_the_wrong_current_password_returns_401()
    {
        var (client, _) = await factory.RegisterAndLoginAsync();

        var response = await client.PutAsJsonAsync("/api/users/me/password", new
        {
            currentPassword = "NotMine@2026",
            newPassword = "Changed@2027",
            confirmNewPassword = "Changed@2027"
        });

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Changing_the_password_signs_out_other_sessions_but_not_the_current_one()
    {
        var (current, profile) = await factory.RegisterAndLoginAsync();
        var otherDevice = await factory.CreateClientForAsync(profile.Email);

        await (await current.PutAsJsonAsync("/api/users/me/password", new
        {
            currentPassword = TestUsers.Password,
            newPassword = "Changed@2027",
            confirmNewPassword = "Changed@2027"
        })).ShouldHaveStatusAsync(HttpStatusCode.NoContent);

        await (await current.GetAsync("/api/users/me")).ShouldHaveStatusAsync(HttpStatusCode.OK);
        await (await otherDevice.GetAsync("/api/users/me")).ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task An_Organiser_can_list_accounts_filtered_by_role()
    {
        var organiser = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var page = await organiser.GetFromJsonAsync<PagedResult<UserProfileDto>>("/api/users?role=participant&pageSize=100");

        Assert.NotEmpty(page!.Items);
        Assert.All(page.Items, u => Assert.Equal("Participant", u.Role));
        Assert.Contains(page.Items, u => u.Email == TestUsers.Sipho);
        Assert.True(page.TotalCount >= 6);
    }

    [Fact]
    public async Task An_Organiser_can_look_up_a_profile_by_id()
    {
        var organiser = await factory.CreateClientForAsync(TestUsers.Thandeka);

        var found = await organiser.GetFromJsonAsync<UserProfileDto>("/api/users/4");
        var missing = await organiser.GetAsync("/api/users/9999");

        Assert.Equal(TestUsers.Lerato, found!.Email);
        await missing.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_Organiser_with_an_upcoming_published_event_cannot_deactivate_their_account()
    {
        var pieter = await factory.CreateClientForAsync(TestUsers.Pieter);

        var response = await pieter.DeleteAsync("/api/users/me");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        await (await pieter.GetAsync("/api/users/me")).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }
}
