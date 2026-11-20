using System.Net.Http.Headers;
using RaceDay.Web.Session;

namespace RaceDay.Web.ApiClient;

public sealed class BearerTokenHandler(IHttpContextAccessor accessor, TokenRefresher refresher, TimeProvider clock) : DelegatingHandler
{
    private static readonly TimeSpan RefreshMargin = TimeSpan.FromMinutes(1);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var anonymous = request.Options.TryGetValue(ApiRequestOptions.Anonymous, out var flag) && flag;
        var session = accessor.HttpContext?.TryGetSession();

        if (!anonymous && session?.GetUser() is { } user)
        {
            if (user.AccessTokenExpiresAt - RefreshMargin <= clock.GetUtcNow().UtcDateTime)
            {
                var tokens = await refresher.RefreshAsync(user.RefreshToken, cancellationToken);
                if (tokens is null)
                {
                    session.Clear();
                    throw new ApiSessionExpiredException("Your session has expired. Please sign in again.");
                }

                user.Apply(tokens);
                session.SetUser(user);
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", user.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
