using System.Net;

namespace RaceDay.Web.ApiClient;

public class ApiResult
{
    private static readonly IReadOnlyDictionary<string, string[]> NoFieldErrors = new Dictionary<string, string[]>();

    public HttpStatusCode StatusCode { get; init; }

    public string? Error { get; init; }

    public IReadOnlyDictionary<string, string[]> FieldErrors { get; init; } = NoFieldErrors;

    public bool Succeeded => (int)StatusCode is >= 200 and <= 299;

    public string ErrorMessage =>
        Error
        ?? FieldErrors.Values.SelectMany(messages => messages).FirstOrDefault()
        ?? "The request could not be completed.";
}

public sealed class ApiResult<T> : ApiResult
{
    public T? Value { get; init; }
}

public sealed class ApiUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

public sealed class ApiSessionExpiredException(string message) : Exception(message);

public sealed class ApiRequestFailedException(ApiResult result) : Exception(result.ErrorMessage)
{
    public ApiResult Result { get; } = result;
}

public static class ApiResultExtensions
{
    public static T Require<T>(this ApiResult<T> result) =>
        result.Succeeded && result.Value is not null ? result.Value : throw new ApiRequestFailedException(result);

    public static void Require(this ApiResult result)
    {
        if (!result.Succeeded)
        {
            throw new ApiRequestFailedException(result);
        }
    }
}
