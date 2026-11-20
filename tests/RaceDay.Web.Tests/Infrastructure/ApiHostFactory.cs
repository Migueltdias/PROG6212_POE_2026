extern alias ApiHost;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ApiDbContext = ApiHost::RaceDay.Api.Data.RaceDayDbContext;
using ApiProgram = ApiHost::Program;
using IImageStorage = ApiHost::RaceDay.Api.Storage.IImageStorage;
using ImageKind = ApiHost::RaceDay.Api.Storage.ImageKind;
using JwtSettings = ApiHost::RaceDay.Api.Security.JwtSettings;

namespace RaceDay.Web.Tests.Infrastructure;

public sealed class ApiHostFactory : WebApplicationFactory<ApiProgram>
{
    private readonly string _databaseName = $"RaceDayWebTests-{Guid.NewGuid():N}";

    public FakeImageStorage Images { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApiDbContext>>();
            services.AddDbContext<ApiDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TestClock.Instance);

            services.PostConfigure<JwtSettings>(settings => settings.SigningKey = "raceday-web-test-signing-key-long-enough-for-hs256");

            services.RemoveAll<IImageStorage>();
            services.AddSingleton<IImageStorage>(Images);
        });
    }

    public async Task<T> WithDbAsync<T>(Func<ApiDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        return await query(scope.ServiceProvider.GetRequiredService<ApiDbContext>());
    }

    public Task<string?> EnrolmentStatusAsync(int participantId, int categoryId) =>
        WithDbAsync(db => db.Enrolments
            .Where(e => e.ParticipantId == participantId && e.CategoryId == categoryId)
            .Select(e => e.EnrolmentStatus)
            .SingleOrDefaultAsync());

    public Task<string?> EventBannerAsync(int eventId) =>
        WithDbAsync(db => db.Events.Where(e => e.EventId == eventId).Select(e => e.BannerImageUrl).SingleAsync());

    public sealed class FakeImageStorage : IImageStorage
    {
        public const string BaseUrl = "https://raceday.blob.test";

        private readonly ConcurrentDictionary<string, byte[]> _blobs = new();

        public IReadOnlyCollection<string> StoredUrls => _blobs.Keys.ToList();

        public async Task<string> UploadAsync(ImageKind kind, string blobName, Stream content, string contentType, CancellationToken cancellationToken = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            var url = $"{BaseUrl}/{(kind == ImageKind.EventBanner ? "event-banners" : "profile-pictures")}/{blobName}";
            _blobs[url] = buffer.ToArray();
            return url;
        }

        public Task DeleteAsync(string storedUrl, CancellationToken cancellationToken = default)
        {
            _blobs.TryRemove(storedUrl, out _);
            return Task.CompletedTask;
        }

        public string? GetReadUrl(string? storedUrl) => storedUrl is null ? null : storedUrl + "?sig=test";
    }
}
