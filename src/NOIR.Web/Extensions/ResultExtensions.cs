namespace NOIR.Web.Extensions;

/// <summary>
/// Extension methods to convert Result to HTTP responses for Minimal APIs.
/// Maps ErrorType to appropriate HTTP status codes.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result to an IResult for Minimal APIs.
    /// Returns 200 OK for success, or appropriate error status for failures.
    /// </summary>
    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess
            ? Results.Ok()
            : ToProblemResult(result.Error);

    /// <summary>
    /// Converts a generic Result to an IResult for Minimal APIs.
    /// Returns 200 OK with the value for success, or appropriate error status for failures.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblemResult(result.Error);

    /// <summary>
    /// Converts a generic Result to an IResult with a custom success handler.
    /// Useful for returning 201 Created or other custom success responses.
    /// </summary>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult> onSuccess) =>
        result.IsSuccess
            ? onSuccess(result.Value)
            : ToProblemResult(result.Error);

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
    /// </summary>
    private static IResult ToProblemResult(Error error) =>
        error.Type switch
        {
            ErrorType.Validation => Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Validation Error",
                detail: error.Message,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"),

            ErrorType.NotFound => Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: error.Message,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.4"),

            ErrorType.Unauthorized => Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: error.Message,
                type: "https://tools.ietf.org/html/rfc7235#section-3.1"),

            ErrorType.Forbidden => Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: error.Message,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.3"),

            ErrorType.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: error.Message,
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.8"),

            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error",
                detail: error.Message,
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.1")
        };
}
