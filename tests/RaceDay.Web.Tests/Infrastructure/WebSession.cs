using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace RaceDay.Web.Tests.Infrastructure;

public sealed partial class WebSession(HttpClient client)
{
    public const string Password = "RaceDay@2026";

    public HttpClient Client { get; } = client;

    public Task<HttpResponseMessage> GetAsync(string url) => Client.GetAsync(url);

    public async Task<string> GetPageAsync(string url)
    {
        var response = await Client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"GET {url} returned {(int)response.StatusCode}: {Excerpt(body)}");
        return body;
    }

    public async Task<HttpResponseMessage> PostFormAsync(string pageUrl, string postUrl, IEnumerable<KeyValuePair<string, string>> fields)
    {
        var token = await TokenFromAsync(pageUrl);
        var form = new List<KeyValuePair<string, string>>(fields) { new("__RequestVerificationToken", token) };
        return await Client.PostAsync(postUrl, new FormUrlEncodedContent(form));
    }

    public async Task<HttpResponseMessage> PostMultipartAsync(string pageUrl, string postUrl, IEnumerable<KeyValuePair<string, string>> fields,
        string fileField, byte[] file, string fileName, string contentType)
    {
        var token = await TokenFromAsync(pageUrl);
        var form = new MultipartFormDataContent { { new StringContent(token), "__RequestVerificationToken" } };
        foreach (var (name, value) in fields)
        {
            form.Add(new StringContent(value), name);
        }

        var content = new ByteArrayContent(file);
        content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(content, fileField, fileName);
        return await Client.PostAsync(postUrl, form);
    }

    public async Task<HttpResponseMessage> LoginAsync(string email, string password = Password)
    {
        var response = await PostFormAsync("/Account/Login", "/Account/Login", Fields(("Email", email), ("Password", password)));
        return response;
    }

    public async Task<string> TokenFromAsync(string pageUrl)
    {
        var page = await GetPageAsync(pageUrl);
        var match = TokenPattern().Match(page);
        Assert.True(match.Success, $"No antiforgery token on {pageUrl}.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    public static IEnumerable<KeyValuePair<string, string>> Fields(params (string Name, string Value)[] fields) =>
        fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value));

    private static string Excerpt(string body) => body.Length > 400 ? body[..400] : body;

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();
}

public static class ResponseAssertions
{
    public static void ShouldRedirectTo(this HttpResponseMessage response, string expectedPathAndQuery)
    {
        Assert.True(response.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found,
            $"Expected a redirect to {expectedPathAndQuery} but got {(int)response.StatusCode}.");
        Assert.Equal(expectedPathAndQuery, response.Headers.Location?.OriginalString);
    }

    public static string RedirectPath(this HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location!.OriginalString;
    }
}
