using System.Diagnostics.CodeAnalysis;

namespace RaceDay.Api.Services;

public enum ServiceErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Unavailable
}

public sealed record ServiceError(ServiceErrorKind Kind, string Message, string? Field = null);

public static class Errors
{
    public static ServiceError Validation(string field, string message) => new(ServiceErrorKind.Validation, message, field);
    public static ServiceError Unauthorized(string message) => new(ServiceErrorKind.Unauthorized, message);
    public static ServiceError Forbidden(string message) => new(ServiceErrorKind.Forbidden, message);
    public static ServiceError NotFound(string message) => new(ServiceErrorKind.NotFound, message);
    public static ServiceError Conflict(string message) => new(ServiceErrorKind.Conflict, message);
    public static ServiceError Unavailable(string message) => new(ServiceErrorKind.Unavailable, message);
}

public class ServiceResult
{
    protected ServiceResult(ServiceError? error) => Error = error;

    public ServiceError? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool Succeeded => Error is null;

    public static ServiceResult Success() => new(null);

    public static implicit operator ServiceResult(ServiceError error) => new(error);
}

public sealed class ServiceResult<T> : ServiceResult
{
    private ServiceResult(T? value, ServiceError? error) : base(error) => Value = value;

    public T? Value { get; }

    public static implicit operator ServiceResult<T>(T value) => new(value, null);

    public static implicit operator ServiceResult<T>(ServiceError error) => new(default, error);
}
