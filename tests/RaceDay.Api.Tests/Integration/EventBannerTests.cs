using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public class EventBannerTests(RaceDayApiFactory factory) : IClassFixture<RaceDayApiFactory>
{
    private Task<HttpClient> Thandeka() => factory.CreateCachedClientForAsync(TestUsers.Thandeka);

    private async Task<EventDetailDto> NewEventAsync(bool publish = false) =>
        await TestData.CreateEventAsync(await Thandeka(), publish, TestData.NewCategory());

    [Fact]
    public async Task The_owner_can_upload_a_banner_which_is_stored_in_blob_storage_and_saved_on_the_event()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();

        var response = await organiser.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var updated = await response.ReadAsync<EventDetailDto>();
        Assert.NotNull(updated.BannerImageUrl);
        Assert.StartsWith($"{InMemoryImageStorage.BaseUrl}/event-banners/events/{ev.EventId}/", updated.BannerImageUrl);
        Assert.EndsWith(".png" + InMemoryImageStorage.ReadSuffix, updated.BannerImageUrl);

        var storedUrl = InMemoryImageStorage.StoredUrlOf(updated.BannerImageUrl);
        var blob = factory.Images.Get(storedUrl);
        Assert.Equal(TestImages.Png, blob.Bytes);
        Assert.Equal("image/png", blob.ContentType);

        var saved = await factory.WithDbAsync(db => db.Events.SingleAsync(e => e.EventId == ev.EventId));
        Assert.Equal(storedUrl, saved.BannerImageUrl);
    }

    [Fact]
    public async Task The_type_is_read_from_the_file_contents_not_its_name()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();

        var response = await organiser.PutAsync($"/api/events/{ev.EventId}/banner",
            TestImages.Form(TestImages.Jpeg, fileName: "banner.png", contentType: "image/png"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var updated = await response.ReadAsync<EventDetailDto>();
        var blob = factory.Images.Get(InMemoryImageStorage.StoredUrlOf(updated.BannerImageUrl!));
        Assert.Equal("image/jpeg", blob.ContentType);
        Assert.EndsWith(".jpg" + InMemoryImageStorage.ReadSuffix, updated.BannerImageUrl);
    }

    [Fact]
    public async Task Replacing_a_banner_deletes_the_old_image()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();

        var first = await (await organiser.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png)))
            .ReadAsync<EventDetailDto>();
        var second = await (await organiser.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Jpeg, "new.jpg", "image/jpeg")))
            .ReadAsync<EventDetailDto>();

        Assert.NotEqual(first.BannerImageUrl, second.BannerImageUrl);
        Assert.False(factory.Images.Contains(InMemoryImageStorage.StoredUrlOf(first.BannerImageUrl!)));
        Assert.True(factory.Images.Contains(InMemoryImageStorage.StoredUrlOf(second.BannerImageUrl!)));
    }

    [Fact]
    public async Task Browsing_and_event_details_return_a_readable_banner_link()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync(publish: true);
        var uploaded = await (await organiser.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png)))
            .ReadAsync<EventDetailDto>();

        var anonymous = factory.CreateAnonymousClient();
        var detail = await anonymous.GetFromJsonAsync<EventDetailDto>($"/api/events/{ev.EventId}");
        var browse = await anonymous.GetFromJsonAsync<PagedResult<EventSummaryDto>>($"/api/events?search={Uri.EscapeDataString(ev.EventName)}");
        var mine = await organiser.GetFromJsonAsync<PagedResult<EventSummaryDto>>("/api/events/mine?pageSize=100");

        Assert.Equal(uploaded.BannerImageUrl, detail!.BannerImageUrl);
        Assert.Equal(uploaded.BannerImageUrl, Assert.Single(browse!.Items).BannerImageUrl);
        Assert.Equal(uploaded.BannerImageUrl, mine!.Items.Single(e => e.EventId == ev.EventId).BannerImageUrl);
    }

    [Fact]
    public async Task A_file_that_is_not_an_image_is_refused()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();

        var response = await organiser.PutAsync($"/api/events/{ev.EventId}/banner",
            TestImages.Form(TestImages.NotAnImage, "banner.png", "image/png"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var problem = await response.ReadValidationProblemAsync();
        Assert.Contains("JPEG, PNG, GIF or WebP", Assert.Single(problem.Errors["file"]));
    }

    [Fact]
    public async Task An_image_over_5_MB_is_refused()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();

        var response = await organiser.PutAsync($"/api/events/{ev.EventId}/banner",
            TestImages.Form(TestImages.PngOfSize(5 * 1024 * 1024 + 1)));

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var problem = await response.ReadValidationProblemAsync();
        Assert.Contains("5 MB", Assert.Single(problem.Errors["file"]));
    }

    [Fact]
    public async Task A_request_without_a_file_is_refused()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();

        var response = await organiser.PutAsync($"/api/events/{ev.EventId}/banner", new MultipartFormDataContent { { new StringContent("x"), "other" } });

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var problem = await response.ReadValidationProblemAsync();
        Assert.True(problem.Errors.ContainsKey("file"));
    }

    [Fact]
    public async Task Only_the_owner_can_change_the_banner()
    {
        var ev = await NewEventAsync();
        var pieter = await factory.CreateCachedClientForAsync(TestUsers.Pieter);
        var ayesha = await factory.CreateCachedClientForAsync(TestUsers.Ayesha);

        var otherOrganiser = await pieter.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png));
        var participant = await ayesha.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png));
        var anonymous = await factory.CreateAnonymousClient().PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png));
        var removeByOther = await pieter.DeleteAsync($"/api/events/{ev.EventId}/banner");

        await otherOrganiser.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
        await participant.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
        await anonymous.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        await removeByOther.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task An_unknown_event_is_not_found()
    {
        var organiser = await Thandeka();

        var response = await organiser.PutAsync("/api/events/99999/banner", TestImages.Form(TestImages.Png));

        await response.ShouldHaveStatusAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_cancelled_event_can_no_longer_change_its_banner()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();
        await organiser.PatchAsJsonAsync($"/api/events/{ev.EventId}/status", new { status = "Cancelled" });

        var response = await organiser.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task When_blob_storage_is_down_the_upload_fails_cleanly_and_nothing_changes()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();

        factory.Images.Unavailable = true;
        HttpResponseMessage response;
        try
        {
            response = await organiser.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png));
        }
        finally
        {
            factory.Images.Unavailable = false;
        }

        await response.ShouldHaveStatusAsync(HttpStatusCode.ServiceUnavailable);
        var saved = await factory.WithDbAsync(db => db.Events.SingleAsync(e => e.EventId == ev.EventId));
        Assert.Null(saved.BannerImageUrl);
    }

    [Fact]
    public async Task Removing_the_banner_deletes_the_image_and_clears_the_url()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();
        var uploaded = await (await organiser.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png)))
            .ReadAsync<EventDetailDto>();

        var response = await organiser.DeleteAsync($"/api/events/{ev.EventId}/banner");
        var again = await organiser.DeleteAsync($"/api/events/{ev.EventId}/banner");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        await again.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        Assert.False(factory.Images.Contains(InMemoryImageStorage.StoredUrlOf(uploaded.BannerImageUrl!)));
        var detail = await organiser.GetFromJsonAsync<EventDetailDto>($"/api/events/{ev.EventId}");
        Assert.Null(detail!.BannerImageUrl);
    }

    [Fact]
    public async Task Deleting_an_event_deletes_its_banner()
    {
        var organiser = await Thandeka();
        var ev = await NewEventAsync();
        var uploaded = await (await organiser.PutAsync($"/api/events/{ev.EventId}/banner", TestImages.Form(TestImages.Png)))
            .ReadAsync<EventDetailDto>();

        var response = await organiser.DeleteAsync($"/api/events/{ev.EventId}");

        await response.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        Assert.False(factory.Images.Contains(InMemoryImageStorage.StoredUrlOf(uploaded.BannerImageUrl!)));
    }
}
