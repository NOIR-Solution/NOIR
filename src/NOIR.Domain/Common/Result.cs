namespace NOIR.Domain.Common;

/// <summary>
/// Represents the result of an operation that can succeed or fail.
/// Provides a functional approach to error handling without exceptions.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Success result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Failure result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>
    /// Creates a validation failure result with multiple errors.
    /// </summary>
    public static Result ValidationFailure(IDictionary<string, string[]> errors) =>
        new(false, Error.ValidationErrors(errors));
}

/// <summary>
/// Represents the result of an operation that can succeed with a value or fail.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result.");

    internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}

/// <summary>
/// Error types for HTTP status code mapping.
/// </summary>
public enum ErrorType
{
    /// <summary>Generic failure (500 Internal Server Error)</summary>
    Failure = 0,
    /// <summary>Validation errors (400 Bad Request)</summary>
    Validation = 1,
    /// <summary>Resource not found (404 Not Found)</summary>
    NotFound = 2,
    /// <summary>Resource conflict (409 Conflict)</summary>
    Conflict = 3,
    /// <summary>Authentication required (401 Unauthorized)</summary>
    Unauthorized = 4,
    /// <summary>Permission denied (403 Forbidden)</summary>
    Forbidden = 5
}

/// <summary>
/// Represents an error with a code, message, and type for HTTP mapping.
/// Error codes follow the format: NOIR-{CATEGORY}-{NUMBER}
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    /// <summary>Represents no error (for successful results).</summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>Represents a null value error.</summary>
    public static readonly Error NullValue = new(ErrorCodes.Validation.Required, "The specified result value is null.");

    /// <summary>Creates a not found error for an entity with a specific error code.</summary>
    public static Error NotFound(string entity, object id, string? code = null) =>
        new(code ?? ErrorCodes.Business.NotFound, $"{entity} with id '{id}' was not found.", ErrorType.NotFound);

    /// <summary>Creates a not found error with a custom message.</summary>
    public static Error NotFound(string message, string? code = null) =>
        new(code ?? ErrorCodes.Business.NotFound, message, ErrorType.NotFound);

    /// <summary>Creates a validation error for a property.</summary>
    public static Error Validation(string propertyName, string message, string? code = null) =>
        new(code ?? ErrorCodes.Validation.General, message, ErrorType.Validation);

    /// <summary>Creates a validation error from multiple errors.</summary>
    public static Error ValidationErrors(IDictionary<string, string[]> errors, string? code = null) =>
        new(code ?? ErrorCodes.Validation.General, string.Join("; ", errors.SelectMany(e => e.Value)), ErrorType.Validation);

    /// <summary>Creates a validation error from error messages.</summary>
    public static Error ValidationErrors(IEnumerable<string> errors, string? code = null) =>
        new(code ?? ErrorCodes.Validation.General, string.Join("; ", errors), ErrorType.Validation);

    /// <summary>Creates a conflict error.</summary>
    public static Error Conflict(string message, string? code = null) =>
        new(code ?? ErrorCodes.Business.Conflict, message, ErrorType.Conflict);

    /// <summary>Creates an unauthorized error.</summary>
    public static Error Unauthorized(string message = "Unauthorized access.", string? code = null) =>
        new(code ?? ErrorCodes.Auth.Unauthorized, message, ErrorType.Unauthorized);

    /// <summary>Creates a forbidden error.</summary>
    public static Error Forbidden(string message = "Access forbidden.", string? code = null) =>
        new(code ?? ErrorCodes.Auth.Forbidden, message, ErrorType.Forbidden);

    /// <summary>Creates a generic failure error.</summary>
    public static Error Failure(string code, string message) =>
        new(code, message, ErrorType.Failure);

    /// <summary>Creates an internal system error.</summary>
    public static Error Internal(string message, string? code = null) =>
        new(code ?? ErrorCodes.System.InternalError, message, ErrorType.Failure);
}
