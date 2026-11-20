using System.Net.Sockets;

namespace RaceDay.Api.Tests.Infrastructure;

public sealed class AzuriteFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> Available = new(() =>
    {
        try
        {
            using var client = new TcpClient();
            return client.ConnectAsync("127.0.0.1", 10000).Wait(TimeSpan.FromSeconds(1)) && client.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    });

    public AzuriteFactAttribute()
    {
        var required = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RACEDAY_REQUIRE_AZURITE"));
        if (!required && !Available.Value)
        {
            Skip = "Azurite is not running on 127.0.0.1:10000 (start it with: npx azurite-blob --silent).";
        }
    }
}
