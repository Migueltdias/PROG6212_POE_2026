using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using RaceDay.Api.Storage;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Integration;

public sealed class AzuriteStorageTests : IAsyncDisposable
{
    private const string ConnectionString = "UseDevelopmentStorage=true";

    private readonly BlobStorageSettings _settings = new()
    {
        ConnectionString = ConnectionString,
        EventBannersContainer = $"test-banners-{Guid.NewGuid():N}",
        ProfilePicturesContainer = $"test-pictures-{Guid.NewGuid():N}"
    };

    private readonly HttpClient _browser = new();

    private AzureBlobImageStorage Storage() => new(Options.Create(_settings), TimeProvider.System);

    [AzuriteFact]
    public async Task An_uploaded_image_can_be_read_back_through_its_signed_url()
    {
        var storage = Storage();

        var storedUrl = await storage.UploadAsync(ImageKind.EventBanner, "events/7/banner.png", new MemoryStream(TestImages.Png), "image/png");
        var response = await _browser.GetAsync(storage.GetReadUrl(storedUrl));

        Assert.StartsWith($"http://127.0.0.1:10000/devstoreaccount1/{_settings.EventBannersContainer}/events/7/banner.png", storedUrl);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(TestImages.Png, await response.Content.ReadAsByteArrayAsync());
    }

    [AzuriteFact]
    public async Task The_containers_are_private_so_the_unsigned_url_is_refused()
    {
        var storage = Storage();

        var storedUrl = await storage.UploadAsync(ImageKind.ProfilePicture, "users/3/me.png", new MemoryStream(TestImages.Png), "image/png");
        var response = await _browser.GetAsync(storedUrl);

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [AzuriteFact]
    public async Task A_deleted_image_is_gone()
    {
        var storage = Storage();
        var storedUrl = await storage.UploadAsync(ImageKind.EventBanner, "events/8/banner.png", new MemoryStream(TestImages.Png), "image/png");

        await storage.DeleteAsync(storedUrl);
        var response = await _browser.GetAsync(storage.GetReadUrl(storedUrl));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public async ValueTask DisposeAsync()
    {
        _browser.Dispose();
        try
        {
            var service = new BlobServiceClient(ConnectionString);
            await service.GetBlobContainerClient(_settings.EventBannersContainer).DeleteIfExistsAsync();
            await service.GetBlobContainerClient(_settings.ProfilePicturesContainer).DeleteIfExistsAsync();
        }
        catch (Exception)
        {
        }
    }
}
