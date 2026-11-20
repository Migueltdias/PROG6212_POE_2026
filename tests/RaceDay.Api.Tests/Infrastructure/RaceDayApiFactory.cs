using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Security;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Tests.Infrastructure;

public class RaceDayApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Today = new(2026, 10, 1, 8, 0, 0, TimeSpan.Zero);

    public const string SigningKey = "raceday-test-signing-key-that-is-long-enough-for-hs256";

    private readonly string _databaseName = $"RaceDayTests-{Guid.NewGuid():N}";
    private readonly ConcurrentDictionary<string, string> _tokens = new();

    public FixedTimeProvider Clock { get; } = new(Today);

    public InMemoryImageStorage Images { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

            services.PostConfigure<JwtSettings>(settings => settings.SigningKey = SigningKey);

            services.RemoveAll<IImageStorage>();
            services.AddSingleton<IImageStorage>(Images);
        });
    }

    public HttpClient CreateAnonymousClient() => CreateClient();

    public async Task<HttpClient> CreateClientForAsync(string email, string password = TestUsers.Password)
    {
        var client = CreateClient();
        var login = await client.LoginAsync(email, password);
        client.UseToken(login.Token);
        return client;
    }

    public async Task<HttpClient> CreateCachedClientForAsync(string email)
    {
        if (!_tokens.TryGetValue(email, out var token))
        {
            token = (await CreateClient().LoginAsync(email, TestUsers.Password)).Token;
            _tokens[email] = token;
        }

        var client = CreateClient();
        client.UseToken(token);
        return client;
    }

    public async Task<T> WithDbAsync<T>(Func<RaceDayDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        return await action(db);
    }

    public Task WithDbAsync(Func<RaceDayDbContext, Task> action) =>
        WithDbAsync(async db =>
        {
            await action(db);
            return true;
        });
}
