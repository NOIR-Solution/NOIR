namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Provides ambient context for capturing exceptions that occur during handler execution.
/// Used to communicate exception information between ExceptionHandlingMiddleware and HandlerAuditMiddleware.
/// </summary>
/// <remarks>
/// This pattern is used because Wolverine's Finally() method doesn't receive exception information,
/// so we need an alternative way to pass exception data to the audit middleware for failure marking.
/// Uses AsyncLocal to ensure thread-safety across async call chains.
/// </remarks>
public static class AuditExceptionContext
{
    private static readonly AsyncLocal<ExceptionContextData?> CurrentData = new();

    /// <summary>
    /// Gets the current exception context data for this async execution context.
    /// </summary>
    public static ExceptionContextData? Current => CurrentData.Value;

    /// <summary>
    /// Sets the exception information in the current context.
    /// Call this from ExceptionHandlingMiddleware when an exception is caught.
    /// </summary>
    /// <param name="exception">The exception that was caught.</param>
    /// <param name="correlationId">Optional correlation ID for linking to audit logs.</param>
    public static void SetException(Exception exception, string? correlationId = null)
    {
        CurrentData.Value = new ExceptionContextData
        {
            Exception = exception,
            CorrelationId = correlationId,
            CapturedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Clears the exception context. Call this after the exception has been processed.
    /// </summary>
    public static void Clear() => CurrentData.Value = null;
}

/// <summary>
/// Data class holding exception information in the ambient context.
/// </summary>
public class ExceptionContextData
{
    /// <summary>
    /// The exception that was caught.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// The correlation ID from the HTTP request, if available.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// When the exception was captured.
    /// </summary>
    public DateTimeOffset CapturedAt { get; init; }
}
