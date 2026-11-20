using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.ApiClient;

public sealed class RaceDayApiClient(HttpClient http, ILogger<RaceDayApiClient> logger) : IRaceDayApi
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task<ApiResult<UserProfile>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<UserProfile>(HttpMethod.Post, "api/auth/register", request, anonymous: true, cancellationToken);

    public Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<LoginResponse>(HttpMethod.Post, "api/auth/login", request, anonymous: true, cancellationToken);

    public Task<ApiResult<TokenResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default) =>
        SendAsync<TokenResponse>(HttpMethod.Post, "api/auth/refresh", new { refreshToken }, anonymous: true, cancellationToken);

    public Task<ApiResult> LogoutAsync(CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/auth/logout", null, cancellationToken);

    public Task<ApiResult<UserProfile>> GetMyProfileAsync(CancellationToken cancellationToken = default) =>
        SendAsync<UserProfile>(HttpMethod.Get, "api/users/me", null, anonymous: false, cancellationToken);

    public Task<ApiResult<UserProfile>> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<UserProfile>(HttpMethod.Put, "api/users/me", request, anonymous: false, cancellationToken);

    public Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, "api/users/me/password", request, cancellationToken);

    public Task<ApiResult<UserProfile>> UploadProfilePictureAsync(Stream image, string fileName, string contentType, CancellationToken cancellationToken = default) =>
        SendAsync<UserProfile>(HttpMethod.Put, "api/users/me/profile-picture", ImageForm(image, fileName, contentType), anonymous: false, cancellationToken);

    public Task<ApiResult> RemoveProfilePictureAsync(CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, "api/users/me/profile-picture", null, cancellationToken);

    public Task<ApiResult<PagedResult<EventSummary>>> BrowseEventsAsync(EventQuery query, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<EventSummary>>(HttpMethod.Get, "api/events" + Query(
            ("province", query.Province), ("discipline", query.Discipline), ("fromDate", query.FromDate),
            ("toDate", query.ToDate), ("search", query.Search), ("page", query.Page), ("pageSize", query.PageSize)),
            null, anonymous: false, cancellationToken);

    public Task<ApiResult<EventDetail>> GetEventAsync(int eventId, CancellationToken cancellationToken = default) =>
        SendAsync<EventDetail>(HttpMethod.Get, $"api/events/{eventId}", null, anonymous: false, cancellationToken);

    public Task<ApiResult<PagedResult<EventSummary>>> GetMyEventsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<EventSummary>>(HttpMethod.Get, "api/events/mine" + Query(("status", status), ("page", page), ("pageSize", pageSize)),
            null, anonymous: false, cancellationToken);

    public Task<ApiResult<EventDetail>> CreateEventAsync(CreateEventRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<EventDetail>(HttpMethod.Post, "api/events", request, anonymous: false, cancellationToken);

    public Task<ApiResult<EventDetail>> UpdateEventAsync(int eventId, EventRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<EventDetail>(HttpMethod.Put, $"api/events/{eventId}", request, anonymous: false, cancellationToken);

    public Task<ApiResult<EventDetail>> ChangeEventStatusAsync(int eventId, string status, CancellationToken cancellationToken = default) =>
        SendAsync<EventDetail>(HttpMethod.Patch, $"api/events/{eventId}/status", new { status }, anonymous: false, cancellationToken);

    public Task<ApiResult> DeleteEventAsync(int eventId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/events/{eventId}", null, cancellationToken);

    public Task<ApiResult<EventDetail>> UploadEventBannerAsync(int eventId, Stream image, string fileName, string contentType, CancellationToken cancellationToken = default) =>
        SendAsync<EventDetail>(HttpMethod.Put, $"api/events/{eventId}/banner", ImageForm(image, fileName, contentType), anonymous: false, cancellationToken);

    public Task<ApiResult> RemoveEventBannerAsync(int eventId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/events/{eventId}/banner", null, cancellationToken);

    public Task<ApiResult<PagedResult<Enrolment>>> GetEventEnrolmentsAsync(int eventId, int? categoryId, string? status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<Enrolment>>(HttpMethod.Get, $"api/events/{eventId}/enrolments" + Query(
            ("categoryId", categoryId), ("status", status), ("page", page), ("pageSize", pageSize)),
            null, anonymous: false, cancellationToken);

    public Task<ApiResult<EventResults>> GetEventResultsAsync(int eventId, CancellationToken cancellationToken = default) =>
        SendAsync<EventResults>(HttpMethod.Get, $"api/events/{eventId}/results", null, anonymous: false, cancellationToken);

    public Task<ApiResult<WeatherSnapshot>> GetEventWeatherAsync(int eventId, CancellationToken cancellationToken = default) =>
        SendAsync<WeatherSnapshot>(HttpMethod.Get, $"api/events/{eventId}/weather", null, anonymous: false, cancellationToken);

    public Task<ApiResult<Category>> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default) =>
        SendAsync<Category>(HttpMethod.Get, $"api/categories/{categoryId}", null, anonymous: false, cancellationToken);

    public Task<ApiResult<Category>> CreateCategoryAsync(int eventId, CategoryRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<Category>(HttpMethod.Post, $"api/events/{eventId}/categories", request, anonymous: false, cancellationToken);

    public Task<ApiResult<Category>> UpdateCategoryAsync(int categoryId, CategoryRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<Category>(HttpMethod.Put, $"api/categories/{categoryId}", request, anonymous: false, cancellationToken);

    public Task<ApiResult> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/categories/{categoryId}", null, cancellationToken);

    public Task<ApiResult<PagedResult<Enrolment>>> GetCategoryEnrolmentsAsync(int categoryId, string? status, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<Enrolment>>(HttpMethod.Get, $"api/categories/{categoryId}/enrolments" + Query(
            ("status", status), ("page", page), ("pageSize", pageSize)), null, anonymous: false, cancellationToken);

    public Task<ApiResult<PagedResult<LeaderboardEntry>>> GetLeaderboardAsync(int categoryId, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<LeaderboardEntry>>(HttpMethod.Get, $"api/categories/{categoryId}/results" + Query(
            ("page", page), ("pageSize", pageSize)), null, anonymous: false, cancellationToken);

    public Task<ApiResult<Enrolment>> EnterAsync(int categoryId, EnrolmentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<Enrolment>(HttpMethod.Post, $"api/categories/{categoryId}/enrolments", request, anonymous: false, cancellationToken);

    public Task<ApiResult<PagedResult<Enrolment>>> GetMyEnrolmentsAsync(string? status, bool? upcoming, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<Enrolment>>(HttpMethod.Get, "api/enrolments/mine" + Query(
            ("status", status), ("upcoming", upcoming), ("page", page), ("pageSize", pageSize)), null, anonymous: false, cancellationToken);

    public Task<ApiResult<Enrolment>> GetEnrolmentAsync(int enrolmentId, CancellationToken cancellationToken = default) =>
        SendAsync<Enrolment>(HttpMethod.Get, $"api/enrolments/{enrolmentId}", null, anonymous: false, cancellationToken);

    public Task<ApiResult<Enrolment>> UpdateEnrolmentAsync(int enrolmentId, EnrolmentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<Enrolment>(HttpMethod.Put, $"api/enrolments/{enrolmentId}", request, anonymous: false, cancellationToken);

    public Task<ApiResult<Enrolment>> PayAsync(int enrolmentId, PaymentRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<Enrolment>(HttpMethod.Patch, $"api/enrolments/{enrolmentId}/payment", request, anonymous: false, cancellationToken);

    public Task<ApiResult> WithdrawAsync(int enrolmentId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/enrolments/{enrolmentId}", null, cancellationToken);

    public Task<ApiResult<RaceResult>> CaptureResultAsync(int enrolmentId, ResultRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<RaceResult>(HttpMethod.Post, $"api/enrolments/{enrolmentId}/result", request, anonymous: false, cancellationToken);

    public Task<ApiResult<RaceResult>> UpdateResultAsync(int resultId, ResultRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<RaceResult>(HttpMethod.Put, $"api/results/{resultId}", request, anonymous: false, cancellationToken);

    public Task<ApiResult> DeleteResultAsync(int resultId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/results/{resultId}", null, cancellationToken);

    public Task<ApiResult<RaceResult>> GetResultAsync(int resultId, CancellationToken cancellationToken = default) =>
        SendAsync<RaceResult>(HttpMethod.Get, $"api/results/{resultId}", null, anonymous: false, cancellationToken);

    public Task<ApiResult<BulkImportResponse>> ImportResultsAsync(int categoryId, BulkResultsRequest request, CancellationToken cancellationToken = default) =>
        SendAsync<BulkImportResponse>(HttpMethod.Post, $"api/categories/{categoryId}/results/bulk", request, anonymous: false, cancellationToken);

    public Task<ApiResult<PagedResult<RaceResult>>> GetMyResultsAsync(string? discipline, int page, int pageSize, CancellationToken cancellationToken = default) =>
        SendAsync<PagedResult<RaceResult>>(HttpMethod.Get, "api/results/mine" + Query(
            ("discipline", discipline), ("page", page), ("pageSize", pageSize)), null, anonymous: false, cancellationToken);

    private async Task<ApiResult> SendAsync(HttpMethod method, string url, object? body, CancellationToken cancellationToken) =>
        await SendAsync<object>(method, url, body, anonymous: false, cancellationToken);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body, bool anonymous, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Content = body switch
        {
            null => null,
            HttpContent content => content,
            _ => JsonContent.Create(body, body.GetType(), options: Json)
        };

        if (anonymous)
        {
            request.Options.Set(ApiRequestOptions.Anonymous, true);
        }

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "The RaceDay API could not be reached for {Method} {Url}", method, url);
            throw new ApiUnavailableException("The RaceDay service could not be reached.", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "The RaceDay API timed out for {Method} {Url}", method, url);
            throw new ApiUnavailableException("The RaceDay service took too long to respond.", ex);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized && !anonymous && response.Headers.WwwAuthenticate.Count > 0)
            {
                var problem = await ReadProblemAsync(response, cancellationToken);
                throw new ApiSessionExpiredException(problem?.Detail ?? "Your session has ended. Please sign in again.");
            }

            if (response.IsSuccessStatusCode)
            {
                var value = typeof(T) == typeof(object) || response.StatusCode == HttpStatusCode.NoContent
                    ? default
                    : await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken);
                return new ApiResult<T> { StatusCode = response.StatusCode, Value = value };
            }

            var failure = await ReadProblemAsync(response, cancellationToken);
            if ((int)response.StatusCode >= 500)
            {
                logger.LogWarning("The RaceDay API answered {Status} for {Method} {Url}: {Detail}",
                    (int)response.StatusCode, method, url, failure?.Detail);
            }

            return new ApiResult<T>
            {
                StatusCode = response.StatusCode,
                Error = failure?.Detail
                        ?? (failure?.Errors is { Count: > 0 }
                            ? null
                            : failure?.Title ?? $"The RaceDay service answered {(int)response.StatusCode} {response.ReasonPhrase}."),
                FieldErrors = failure?.Errors ?? new Dictionary<string, string[]>()
            };
        }
    }

    private static async Task<ProblemPayload?> ReadProblemAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ProblemPayload>(Json, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }

    private static MultipartFormDataContent ImageForm(Stream image, string fileName, string contentType)
    {
        var file = new StreamContent(image);
        file.Headers.ContentType = MediaTypeHeaderValue.TryParse(contentType, out var type)
            ? type
            : new MediaTypeHeaderValue("application/octet-stream");
        return new MultipartFormDataContent { { file, "file", Path.GetFileName(fileName) } };
    }

    internal static string Query(params (string Name, object? Value)[] parameters)
    {
        var builder = new StringBuilder();
        foreach (var (name, value) in parameters)
        {
            var text = value switch
            {
                null => null,
                string s when string.IsNullOrWhiteSpace(s) => null,
                string s => s.Trim(),
                DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                bool b => b ? "true" : "false",
                IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };

            if (text is null)
            {
                continue;
            }

            builder.Append(builder.Length == 0 ? '?' : '&')
                .Append(Uri.EscapeDataString(name)).Append('=').Append(Uri.EscapeDataString(text));
        }

        return builder.ToString();
    }

    private sealed class ProblemPayload
    {
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }
}
