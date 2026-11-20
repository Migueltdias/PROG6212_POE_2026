namespace RaceDay.Web.ApiClient;

public sealed class RaceDayApiOptions
{
    public const string SectionName = "RaceDayApi";
    public const string HttpClientName = "RaceDayApi";

    public string BaseUrl { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 30;
}

public static class ApiRequestOptions
{
    public static readonly HttpRequestOptionsKey<bool> Anonymous = new("RaceDay.Anonymous");
}
