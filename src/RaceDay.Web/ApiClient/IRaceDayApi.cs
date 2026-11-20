using RaceDay.Web.ApiClient.Contracts;

namespace RaceDay.Web.ApiClient;

public interface IRaceDayApi
{
    Task<ApiResult<UserProfile>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<TokenResponse>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ApiResult> LogoutAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<UserProfile>> GetMyProfileAsync(CancellationToken cancellationToken = default);
    Task<ApiResult<UserProfile>> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<UserProfile>> UploadProfilePictureAsync(Stream image, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ApiResult> RemoveProfilePictureAsync(CancellationToken cancellationToken = default);

    Task<ApiResult<PagedResult<EventSummary>>> BrowseEventsAsync(EventQuery query, CancellationToken cancellationToken = default);
    Task<ApiResult<EventDetail>> GetEventAsync(int eventId, CancellationToken cancellationToken = default);
    Task<ApiResult<PagedResult<EventSummary>>> GetMyEventsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResult<EventDetail>> CreateEventAsync(CreateEventRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<EventDetail>> UpdateEventAsync(int eventId, EventRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<EventDetail>> ChangeEventStatusAsync(int eventId, string status, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteEventAsync(int eventId, CancellationToken cancellationToken = default);
    Task<ApiResult<EventDetail>> UploadEventBannerAsync(int eventId, Stream image, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ApiResult> RemoveEventBannerAsync(int eventId, CancellationToken cancellationToken = default);
    Task<ApiResult<PagedResult<Enrolment>>> GetEventEnrolmentsAsync(int eventId, int? categoryId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResult<EventResults>> GetEventResultsAsync(int eventId, CancellationToken cancellationToken = default);
    Task<ApiResult<WeatherSnapshot>> GetEventWeatherAsync(int eventId, CancellationToken cancellationToken = default);

    Task<ApiResult<Category>> GetCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<ApiResult<Category>> CreateCategoryAsync(int eventId, CategoryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<Category>> UpdateCategoryAsync(int categoryId, CategoryRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<ApiResult<PagedResult<Enrolment>>> GetCategoryEnrolmentsAsync(int categoryId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResult<PagedResult<LeaderboardEntry>>> GetLeaderboardAsync(int categoryId, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<ApiResult<Enrolment>> EnterAsync(int categoryId, EnrolmentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<PagedResult<Enrolment>>> GetMyEnrolmentsAsync(string? status, bool? upcoming, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResult<Enrolment>> GetEnrolmentAsync(int enrolmentId, CancellationToken cancellationToken = default);
    Task<ApiResult<Enrolment>> UpdateEnrolmentAsync(int enrolmentId, EnrolmentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<Enrolment>> PayAsync(int enrolmentId, PaymentRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> WithdrawAsync(int enrolmentId, CancellationToken cancellationToken = default);

    Task<ApiResult<RaceResult>> CaptureResultAsync(int enrolmentId, ResultRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<RaceResult>> UpdateResultAsync(int resultId, ResultRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> DeleteResultAsync(int resultId, CancellationToken cancellationToken = default);
    Task<ApiResult<RaceResult>> GetResultAsync(int resultId, CancellationToken cancellationToken = default);
    Task<ApiResult<BulkImportResponse>> ImportResultsAsync(int categoryId, BulkResultsRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<PagedResult<RaceResult>>> GetMyResultsAsync(string? discipline, int page, int pageSize, CancellationToken cancellationToken = default);
}
