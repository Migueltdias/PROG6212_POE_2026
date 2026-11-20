using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Web.ApiClient;

namespace RaceDay.Web.Tests.Infrastructure;

public sealed class RaceDayWebFactory : WebApplicationFactory<Program>
{
    public ApiHostFactory Api { get; } = new();

    public bool ApiDown { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("RaceDayApi:BaseUrl", "http://raceday-api.test/");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TestClock.Instance);

            services.AddHttpClient(RaceDayApiOptions.HttpClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new SwitchableHandler(this, Api.Server.CreateHandler()));
        });
    }

    public WebSession NewSession() =>
        new(CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true }));

    public async Task<WebSession> SignedInAsync(string email)
    {
        var session = NewSession();
        await session.LoginAsync(email);
        return session;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Api.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class SwitchableHandler(RaceDayWebFactory factory, HttpMessageHandler inner) : DelegatingHandler(inner)
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            factory.ApiDown
                ? throw new HttpRequestException("No connection could be made because the target machine actively refused it.")
                : base.SendAsync(request, cancellationToken);
    }
}
