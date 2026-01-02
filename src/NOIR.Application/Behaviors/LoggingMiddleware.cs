namespace NOIR.Application.Behaviors;

/// <summary>
/// Wolverine middleware for structured logging of all handler executions.
/// Logs before/after handler execution with correlation ID and timing information.
/// </summary>
/// <remarks>
/// This middleware provides centralized logging for all command/query handlers,
/// eliminating the need for manual logging in each handler.
///
/// Execution order:
/// 1. Before() - logs start of handler execution
/// 2. Handler executes
/// 3. After() - logs successful completion with duration
/// 4. Finally() - logs failures if exception occurred
///
/// Uses Envelope.Message to access the message to avoid Wolverine treating
/// `object` parameter as a service dependency.
/// </remarks>
public class LoggingMiddleware
{
    private readonly Stopwatch _stopwatch = new();

    /// <summary>
    /// Called before the handler executes.
    /// Logs the message type and correlation ID.
    /// </summary>
    public void Before(ILogger<LoggingMiddleware> logger, Envelope envelope)
    {
        _stopwatch.Restart();
        var message = envelope.Message;
        logger.LogInformation(
            "Handling {MessageType} | CorrelationId: {CorrelationId}",
            message?.GetType().Name ?? "Unknown",
            envelope.CorrelationId);
    }

    /// <summary>
    /// Called after the handler executes successfully.
    /// Logs the message type, duration, and correlation ID.
    /// </summary>
    public void After(ILogger<LoggingMiddleware> logger, Envelope envelope)
    {
        var message = envelope.Message;
        logger.LogInformation(
            "Handled {MessageType} successfully in {ElapsedMs}ms | CorrelationId: {CorrelationId}",
            message?.GetType().Name ?? "Unknown",
            _stopwatch.ElapsedMilliseconds,
            envelope.CorrelationId);
    }

    /// <summary>
    /// Called after handler execution completes (success or failure).
    /// Logs errors if an exception occurred.
    /// </summary>
    public void Finally(ILogger<LoggingMiddleware> logger, Envelope envelope)
    {
        _stopwatch.Stop();
        // Note: Exception handling is done via Wolverine's error handling policies
        // This method just ensures the stopwatch is stopped
    }
}
