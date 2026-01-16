using NOIR.Infrastructure.Audit;

namespace NOIR.Web.Extensions;

/// <summary>
/// Extension methods to convert Result to HTTP responses for Minimal APIs.
/// Maps ErrorType to appropriate HTTP status codes with RFC 7807 Problem Details.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result to an IResult for Minimal APIs.
    /// Returns 200 OK for success, or appropriate error status for failures.
    /// </summary>
    public static IResult ToHttpResult(this Result result, HttpContext? context = null) =>
        result.IsSuccess
            ? Results.Ok()
            : ToProblemResult(result.Error, context);

    /// <summary>
    /// Converts a generic Result to an IResult for Minimal APIs.
    /// Returns 200 OK with the value for success, or appropriate error status for failures.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext? context = null) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblemResult(result.Error, context);

    /// <summary>
    /// Converts a generic Result to an IResult with a custom success handler.
    /// Useful for returning 201 Created or other custom success responses.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess, HttpContext? context = null) =>
        result.IsSuccess
            ? onSuccess(result.Value)
            : ToProblemResult(result.Error, context);

    /// <summary>
    /// Converts a generic Result to an IResult with custom handlers for both success and failure.
    /// </summary>
    public static IResult Match<T>(
        this Result<T> result,
        Func<T, IResult> onSuccess,
        Func<Error, IResult> onFailure) =>
        result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.Error);

    /// <summary>
    /// Maps an Error to a Problem Details HTTP response.
    /// Includes error code, correlation ID, and timestamp for debugging.
    /// Also sets AuditResultContext to capture the failure for logging middleware.
    /// </summary>
    private static IResult ToProblemResult(Error error, HttpContext? context = null)
    {
        // Set the result context so middleware can detect this business logic failure
        AuditResultContext.SetFailure(error.Message, error.Code);

        var extensions = CreateErrorExtensions(error, context);

        return error.Type switch
        {
            ErrorType.Validation => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error",
                detail: error.Message,
                type: $"https://api.noir.local/errors/{error.Code}",
                extensions: extensions),

            ErrorType.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: error.Message,
                type: $"https://api.noir.local/errors/{error.Code}",
                extensions: extensions),

            ErrorType.Unauthorized => Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: error.Message,
                type: $"https://api.noir.local/errors/{error.Code}",
                extensions: extensions),

            ErrorType.Forbidden => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: error.Message,
                type: $"https://api.noir.local/errors/{error.Code}",
                extensions: extensions),

            ErrorType.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: error.Message,
                type: $"https://api.noir.local/errors/{error.Code}",
                extensions: extensions),

            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: error.Message,
                type: $"https://api.noir.local/errors/{error.Code}",
                extensions: extensions)
        };
    }

    /// <summary>
    /// Creates the extensions dictionary for Problem Details.
    /// </summary>
    private static Dictionary<string, object?> CreateErrorExtensions(Error error, HttpContext? context)
    {
        return new Dictionary<string, object?>
        {
            ["errorCode"] = error.Code,
            ["correlationId"] = context?.TraceIdentifier ?? System.Diagnostics.Activity.Current?.Id ?? Guid.NewGuid().ToString(),
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };
    }
}
