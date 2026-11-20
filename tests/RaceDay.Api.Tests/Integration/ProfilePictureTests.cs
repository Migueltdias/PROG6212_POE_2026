using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class ProfilePictureTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    [Fact]
    public async Task A_user_can_upload_a_profile_picture_which_is_stored_and_saved_on_the_account()
    {
        var (client, profile) = await factory.RegisterAndLoginAsync();

        var response = await client.PutAsync("/api/users/me/profile-picture", TestImages.Form(TestImages.Png));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var updated = await response.ReadAsync<UserProfileDto>();
        Assert.StartsWith($"{InMemoryImageStorage.BaseUrl}/profile-pictures/users/{profile.UserId}/", updated.ProfilePictureUrl);
        Assert.EndsWith(InMemoryImageStorage.ReadSuffix, updated.ProfilePictureUrl);

        var storedUrl = InMemoryImageStorage.StoredUrlOf(updated.ProfilePictureUrl!);
        Assert.Equal(TestImages.Png, factory.Images.Get(storedUrl).Bytes);
        var saved = await factory.WithDbAsync(db => db.Users.SingleAsync(u => u.UserId == profile.UserId));
        Assert.Equal(storedUrl, saved.ProfilePictureUrl);
    }

    [Fact]
    public async Task The_picture_comes_back_on_the_profile_and_at_the_next_login()
    {
        var (client, profile) = await factory.RegisterAndLoginAsync(role: "Organiser", dateOfBirth: new DateOnly(1980, 1, 1));
        var uploaded = await (await client.PutAsync("/api/users/me/profile-picture", TestImages.Form(TestImages.Png)))
            .ReadAsync<UserProfileDto>();

        var me = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");
        var login = await factory.CreateAnonymousClient().LoginAsync(profile.Email, TestUsers.Password);

        Assert.Equal(uploaded.ProfilePictureUrl, me!.ProfilePictureUrl);
        Assert.Equal(uploaded.ProfilePictureUrl, login.User.ProfilePictureUrl);
    }

    [Fact]
    public async Task Replacing_the_picture_deletes_the_old_one()
    {
        var (client, _) = await factory.RegisterAndLoginAsync();

        var first = await (await client.PutAsync("/api/users/me/profile-picture", TestImages.Form(TestImages.Png)))
            .ReadAsync<UserProfileDto>();
        var second = await (await client.PutAsync("/api/users/me/profile-picture", TestImages.Form(TestImages.Jpeg, "me.jpg", "image/jpeg")))
            .ReadAsync<UserProfileDto>();

        Assert.False(factory.Images.Contains(InMemoryImageStorage.StoredUrlOf(first.ProfilePictureUrl!)));
        Assert.True(factory.Images.Contains(InMemoryImageStorage.StoredUrlOf(second.ProfilePictureUrl!)));
    }

    [Fact]
    public async Task A_file_that_is_not_an_image_is_refused()
    {
        var (client, _) = await factory.RegisterAndLoginAsync();

        var response = await client.PutAsync("/api/users/me/profile-picture", TestImages.Form(TestImages.NotAnImage, "me.jpg", "image/jpeg"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var problem = await response.ReadValidationProblemAsync();
        Assert.True(problem.Errors.ContainsKey("file"));
    }

    [Fact]
    public async Task Removing_the_picture_deletes_it_and_clears_the_url()
    {
        var (client, _) = await factory.RegisterAndLoginAsync();
        var uploaded = await (await client.PutAsync("/api/users/me/profile-picture", TestImages.Form(TestImages.Png)))
            .ReadAsync<UserProfileDto>();

        var response = await client.DeleteAsync("/api/users/me/profile-picture");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        Assert.False(factory.Images.Contains(InMemoryImageStorage.StoredUrlOf(uploaded.ProfilePictureUrl!)));
        var me = await client.GetFromJsonAsync<UserProfileDto>("/api/users/me");
        Assert.Null(me!.ProfilePictureUrl);
    }

    [Fact]
    public async Task Uploading_a_picture_needs_a_logged_in_user()
    {
        var anonymous = factory.CreateAnonymousClient();

        var upload = await anonymous.PutAsync("/api/users/me/profile-picture", TestImages.Form(TestImages.Png));
        var remove = await anonymous.DeleteAsync("/api/users/me/profile-picture");

        await upload.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        await remove.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }
}
