using Microsoft.Extensions.Options;
using RaceDay.Api.Storage;
using RaceDay.Api.Tests.Infrastructure;

namespace RaceDay.Api.Tests.Unit;

public class AzureBlobImageStorageTests
{
    public const string AzuriteKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

    private const string LocalBanner = "http://127.0.0.1:10000/devstoreaccount1/event-banners/events/2/banner.png";

    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2026, 10, 1, 15, 30, 0, TimeSpan.Zero));

    private AzureBlobImageStorage Storage(string connectionString = "UseDevelopmentStorage=true", string? publicBaseUrl = null) =>
        new(Options.Create(new BlobStorageSettings { ConnectionString = connectionString, PublicBaseUrl = publicBaseUrl }), _clock);

    [Fact]
    public void A_missing_image_has_no_read_url()
    {
        Assert.Null(Storage().GetReadUrl(null));
        Assert.Null(Storage().GetReadUrl(" "));
    }

    [Fact]
    public void A_stored_blob_url_is_signed_for_reading_only()
    {
        var url = Storage().GetReadUrl(LocalBanner)!;

        Assert.StartsWith(LocalBanner + "?", url);
        Assert.Contains("sp=r&", url);
        Assert.Contains("sig=", url);
    }

    [Fact]
    public void The_signature_expires_two_midnights_ahead_so_the_url_is_stable_all_day()
    {
        var storage = Storage();

        var afternoon = storage.GetReadUrl(LocalBanner);
        _clock.Advance(TimeSpan.FromHours(3));
        var evening = storage.GetReadUrl(LocalBanner);

        Assert.Equal(afternoon, evening);
        Assert.Contains("se=2026-10-03T00%3A00%3A00Z", afternoon);
    }

    [Fact]
    public void The_public_base_url_replaces_the_internal_docker_host()
    {
        var connection = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;" +
                         $"AccountKey={AzuriteKey};BlobEndpoint=http://azurite:10000/devstoreaccount1;";
        var storage = Storage(connection, publicBaseUrl: "http://localhost:10000/devstoreaccount1");

        var url = storage.GetReadUrl("http://azurite:10000/devstoreaccount1/profile-pictures/users/5/me.jpg")!;

        Assert.StartsWith("http://localhost:10000/devstoreaccount1/profile-pictures/users/5/me.jpg?", url);
        Assert.Contains("sig=", url);
    }

    [Fact]
    public void A_url_outside_the_storage_account_is_returned_unchanged()
    {
        const string external = "https://example.com/some/picture.png";

        Assert.Equal(external, Storage().GetReadUrl(external));
    }

    [Fact]
    public void Without_a_connection_string_stored_urls_pass_through_unsigned()
    {
        Assert.Equal(LocalBanner, Storage(connectionString: "").GetReadUrl(LocalBanner));
    }

    [Fact]
    public async Task Without_a_connection_string_an_upload_reports_storage_unavailable()
    {
        var storage = Storage(connectionString: "");

        await Assert.ThrowsAsync<StorageUnavailableException>(() =>
            storage.UploadAsync(ImageKind.EventBanner, "events/1/a.png", new MemoryStream(TestImages.Png), "image/png"));
    }
}
