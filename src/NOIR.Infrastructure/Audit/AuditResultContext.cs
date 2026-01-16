namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Provides ambient context for capturing Result failures that occur during handler execution.
/// Used to communicate business logic failures (Result.Failure) to middleware for proper logging.
/// </summary>
/// <remarks>
/// This pattern is used because Wolverine middleware cannot directly capture handler return values.
/// When an endpoint converts Result.Failure() to an HTTP response, it sets the failure context here.
/// Middleware can then check this context to know if the handler had a business logic failure.
/// Uses AsyncLocal to ensure thread-safety across async call chains.
/// </remarks>
public static class AuditResultContext
{
    private static readonly AsyncLocal<ResultContextData?> CurrentData = new();

    /// <summary>
    /// Gets the current result context data for this async execution context.
    /// </summary>
    public static ResultContextData? Current => CurrentData.Value;

    /// <summary>
    /// Sets a failure result in the current context.
    /// Call this from endpoint code when Result.IsFailure is true.
    /// </summary>
    /// <param name="errorMessage">The error message from the Result.</param>
    /// <param name="errorCode">Optional error code for categorization.</param>
    public static void SetFailure(string errorMessage, string? errorCode = null)
    {
        CurrentData.Value = new ResultContextData
        {
            IsFailure = true,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode,
            CapturedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Checks if the current context has a failure.
    /// </summary>
    public static bool HasFailure => CurrentData.Value?.IsFailure == true;

    /// <summary>
    /// Gets the error message from the current context, or null if no failure.
    /// </summary>
    public static string? ErrorMessage => CurrentData.Value?.ErrorMessage;

    /// <summary>
    /// Clears the result context. Called automatically at the end of the request.
    /// </summary>
    public static void Clear() => CurrentData.Value = null;
}

/// <summary>
/// Data class holding result failure information in the ambient context.
/// </summary>
public class ResultContextData
{
    /// <summary>
    /// Whether the result was a failure.
    /// </summary>
    public bool IsFailure { get; init; }

    /// <summary>
    /// The error message from the Result.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The error code for categorization.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// When the failure was captured.
    /// </summary>
    public DateTimeOffset CapturedAt { get; init; }
}
