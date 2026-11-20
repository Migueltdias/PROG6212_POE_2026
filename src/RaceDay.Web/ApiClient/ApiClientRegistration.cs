using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace RaceDay.Web.ApiClient;

public static class ApiClientRegistration
{
    public static IHttpClientBuilder AddRaceDayApi(this IServiceCollection services)
    {
        services.AddOptions<RaceDayApiOptions>()
            .BindConfiguration(RaceDayApiOptions.SectionName)
            .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _),
                "RaceDayApi:BaseUrl must be an absolute URL, for example https://localhost:7099/.")
            .Validate(o => o.TimeoutSeconds is > 0 and <= 300, "RaceDayApi:TimeoutSeconds must be between 1 and 300.")
            .ValidateOnStart();

        return services.AddHttpClient<IRaceDayApi, RaceDayApiClient>(RaceDayApiOptions.HttpClientName, (provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<RaceDayApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/");
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }
}
