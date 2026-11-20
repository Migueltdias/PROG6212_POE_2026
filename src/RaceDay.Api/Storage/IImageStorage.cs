namespace RaceDay.Api.Storage;

public enum ImageKind
{
    EventBanner,
    ProfilePicture
}

public interface IImageStorage
{
    Task<string> UploadAsync(ImageKind kind, string blobName, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storedUrl, CancellationToken cancellationToken = default);

    string? GetReadUrl(string? storedUrl);
}

public sealed class StorageUnavailableException(string message, Exception? inner = null) : Exception(message, inner);
