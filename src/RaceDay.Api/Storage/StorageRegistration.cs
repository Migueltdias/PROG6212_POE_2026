namespace RaceDay.Api.Storage;

public static class StorageRegistration
{
    public static IServiceCollection AddRaceDayStorage(this IServiceCollection services)
    {
        services.AddOptions<BlobStorageSettings>()
            .BindConfiguration(BlobStorageSettings.SectionName)
            .Validate(s => s.MaxImageBytes > 0, "BlobStorage:MaxImageBytes must be positive.");

        services.AddSingleton<IImageStorage, AzureBlobImageStorage>();
        return services;
    }
}
