using System.Collections.Concurrent;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Tests.Infrastructure;

public sealed class InMemoryImageStorage : IImageStorage
{
    public const string BaseUrl = "https://raceday.blob.test";
    public const string ReadSuffix = "?sig=test";

    private readonly ConcurrentDictionary<string, StoredBlob> _blobs = new();

    public bool Unavailable { get; set; }

    public async Task<string> UploadAsync(ImageKind kind, string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        if (Unavailable)
        {
            throw new StorageUnavailableException("Storage is switched off for this test.");
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        var container = kind == ImageKind.EventBanner ? "event-banners" : "profile-pictures";
        var url = $"{BaseUrl}/{container}/{blobName}";
        _blobs[url] = new StoredBlob(buffer.ToArray(), contentType);
        return url;
    }

    public Task DeleteAsync(string storedUrl, CancellationToken cancellationToken = default)
    {
        _blobs.TryRemove(storedUrl, out _);
        return Task.CompletedTask;
    }

    public string? GetReadUrl(string? storedUrl) => storedUrl is null ? null : storedUrl + ReadSuffix;

    public bool Contains(string storedUrl) => _blobs.ContainsKey(storedUrl);

    public StoredBlob Get(string storedUrl) => _blobs[storedUrl];

    public static string StoredUrlOf(string readUrl) =>
        readUrl.EndsWith(ReadSuffix, StringComparison.Ordinal) ? readUrl[..^ReadSuffix.Length] : readUrl;

    public sealed record StoredBlob(byte[] Bytes, string ContentType);
}
