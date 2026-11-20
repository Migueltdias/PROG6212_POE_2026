using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Storage;

public sealed class AzureBlobImageStorage : IImageStorage
{
    private const string CacheControl = "public, max-age=604800";

    private readonly BlobStorageSettings _settings;
    private readonly TimeProvider _clock;
    private readonly BlobServiceClient? _service;
    private readonly SemaphoreSlim _containerGate = new(1, 1);
    private readonly HashSet<string> _readyContainers = [];

    public AzureBlobImageStorage(IOptions<BlobStorageSettings> options, TimeProvider clock)
    {
        _settings = options.Value;
        _clock = clock;

        if (!string.IsNullOrWhiteSpace(_settings.ConnectionString))
        {
            var clientOptions = new BlobClientOptions();
            clientOptions.Retry.MaxRetries = 2;
            clientOptions.Retry.Delay = TimeSpan.FromMilliseconds(500);
            clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(30);
            _service = new BlobServiceClient(_settings.ConnectionString, clientOptions);
        }
    }

    public async Task<string> UploadAsync(ImageKind kind, string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        var container = Container(kind)
                        ?? throw new StorageUnavailableException("Blob storage is not configured. Set BlobStorage:ConnectionString.");
        try
        {
            await EnsureContainerAsync(container, cancellationToken);

            var blob = container.GetBlobClient(blobName);
            await blob.UploadAsync(content, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType, CacheControl = CacheControl }
            }, cancellationToken);

            return blob.Uri.AbsoluteUri;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not StorageUnavailableException)
        {
            throw new StorageUnavailableException("The image could not be saved to Azure Blob Storage.", ex);
        }
    }

    public async Task DeleteAsync(string storedUrl, CancellationToken cancellationToken = default)
    {
        if (!TryResolve(storedUrl, out var blob))
        {
            return;
        }

        try
        {
            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new StorageUnavailableException("The image could not be removed from Azure Blob Storage.", ex);
        }
    }

    public string? GetReadUrl(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl))
        {
            return null;
        }

        if (!TryResolve(storedUrl, out var blob))
        {
            return storedUrl;
        }

        var uri = blob.Uri;
        if (blob.CanGenerateSasUri)
        {
            var today = _clock.GetUtcNow().UtcDateTime.Date;
            var expiresOn = new DateTimeOffset(today, TimeSpan.Zero).AddDays(2);
            uri = blob.GenerateSasUri(BlobSasPermissions.Read, expiresOn);
        }

        return ToPublicUrl(uri);
    }

    private BlobContainerClient? Container(ImageKind kind) =>
        _service?.GetBlobContainerClient(kind switch
        {
            ImageKind.EventBanner => _settings.EventBannersContainer,
            ImageKind.ProfilePicture => _settings.ProfilePicturesContainer,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });

    private async Task EnsureContainerAsync(BlobContainerClient container, CancellationToken cancellationToken)
    {
        if (_readyContainers.Contains(container.Name))
        {
            return;
        }

        await _containerGate.WaitAsync(cancellationToken);
        try
        {
            if (_readyContainers.Contains(container.Name))
            {
                return;
            }

            await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
            _readyContainers.Add(container.Name);
        }
        finally
        {
            _containerGate.Release();
        }
    }

    private bool TryResolve(string storedUrl, out BlobClient blob)
    {
        foreach (var kind in Enum.GetValues<ImageKind>())
        {
            var container = Container(kind);
            if (container is null)
            {
                break;
            }

            var prefix = container.Uri.AbsoluteUri.TrimEnd('/') + "/";
            if (storedUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && storedUrl.Length > prefix.Length)
            {
                blob = container.GetBlobClient(Uri.UnescapeDataString(storedUrl[prefix.Length..]));
                return true;
            }
        }

        blob = null!;
        return false;
    }

    private string ToPublicUrl(Uri uri)
    {
        var url = uri.AbsoluteUri;
        if (string.IsNullOrWhiteSpace(_settings.PublicBaseUrl) || _service is null)
        {
            return url;
        }

        var accountBase = _service.Uri.AbsoluteUri.TrimEnd('/');
        return url.StartsWith(accountBase, StringComparison.OrdinalIgnoreCase)
            ? _settings.PublicBaseUrl.TrimEnd('/') + url[accountBase.Length..]
            : url;
    }
}
