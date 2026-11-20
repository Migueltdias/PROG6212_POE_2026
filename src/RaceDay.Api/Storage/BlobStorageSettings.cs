namespace RaceDay.Api.Storage;

public sealed class BlobStorageSettings
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string EventBannersContainer { get; set; } = "event-banners";

    public string ProfilePicturesContainer { get; set; } = "profile-pictures";

    public string? PublicBaseUrl { get; set; }

    public long MaxImageBytes { get; set; } = 5 * 1024 * 1024;
}
