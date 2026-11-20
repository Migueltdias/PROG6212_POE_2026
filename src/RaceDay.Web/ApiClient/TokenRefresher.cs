using Microsoft.Extensions.Caching.Memory;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.ApiClient;

public sealed class TokenRefresher(IServiceScopeFactory scopes, IMemoryCache cache)
{
    private static readonly TimeSpan ReuseWindow = TimeSpan.FromMinutes(2);
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<TokenResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var key = $"raceday:refresh:{refreshToken}";
        if (cache.TryGetValue(key, out TokenResponse? reused))
        {
            return reused;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (cache.TryGetValue(key, out reused))
            {
                return reused;
            }

            using var scope = scopes.CreateScope();
            var api = scope.ServiceProvider.GetRequiredService<IRaceDayApi>();
            var result = await api.RefreshAsync(refreshToken, cancellationToken);
            if (!result.Succeeded || result.Value is null)
            {
                return null;
            }

            cache.Set(key, result.Value, ReuseWindow);
            return result.Value;
        }
        finally
        {
            _gate.Release();
        }
    }
}
