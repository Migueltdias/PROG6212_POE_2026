using Microsoft.Extensions.Options;
using RaceDay.Api.Dtos;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Services;

public sealed record ImageUpload(Stream Content, long Length, string? FileName);

public static class ReadableImageExtensions
{
    public static UserProfileDto WithReadablePicture(this UserProfileDto profile, IImageService images)
    {
        profile.ProfilePictureUrl = images.ReadUrl(profile.ProfilePictureUrl);
        return profile;
    }
}

public interface IImageService
{
    Task<ServiceResult<string>> SaveAsync(ImageKind kind, string folder, ImageUpload upload, CancellationToken cancellationToken = default);

    Task RemoveAsync(string? storedUrl, CancellationToken cancellationToken = default);

    string? ReadUrl(string? storedUrl);
}

public sealed class ImageService(
    IImageStorage storage,
    IOptions<BlobStorageSettings> settings,
    ILogger<ImageService> logger) : IImageService
{
    public const string Field = "file";

    public async Task<ServiceResult<string>> SaveAsync(ImageKind kind, string folder, ImageUpload upload, CancellationToken cancellationToken = default)
    {
        var maxBytes = settings.Value.MaxImageBytes;
        if (upload.Length <= 0)
        {
            return Errors.Validation(Field, "Choose an image to upload.");
        }

        if (upload.Length > maxBytes)
        {
            return Errors.Validation(Field, $"The image is {Megabytes(upload.Length)}; the largest allowed is {Megabytes(maxBytes)}.");
        }

        var content = upload.Content;
        if (!content.CanSeek)
        {
            var buffered = new MemoryStream();
            await content.CopyToAsync(buffered, cancellationToken);
            content = buffered;
        }

        content.Position = 0;
        var header = new byte[ImageFormat.HeaderLength];
        var read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var format = ImageFormat.Detect(header.AsSpan(0, read));
        if (format is null)
        {
            return Errors.Validation(Field, "Only JPEG, PNG, GIF or WebP images can be uploaded.");
        }

        content.Position = 0;
        var blobName = $"{folder}/{Guid.NewGuid():N}{format.Extension}";
        try
        {
            return await storage.UploadAsync(kind, blobName, content, format.ContentType, cancellationToken);
        }
        catch (StorageUnavailableException ex)
        {
            logger.LogError(ex, "Uploading {BlobName} failed", blobName);
            return Errors.Unavailable("Image storage is not available right now, so the image was not saved. Try again shortly.");
        }
    }

    public async Task RemoveAsync(string? storedUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedUrl))
        {
            return;
        }

        try
        {
            await storage.DeleteAsync(storedUrl, cancellationToken);
        }
        catch (StorageUnavailableException ex)
        {
            logger.LogWarning(ex, "The old image {Url} could not be deleted and has been left in storage", storedUrl);
        }
    }

    public string? ReadUrl(string? storedUrl) => storage.GetReadUrl(storedUrl);

    private static string Megabytes(long bytes) => $"{bytes / 1024d / 1024d:0.#} MB";
}
