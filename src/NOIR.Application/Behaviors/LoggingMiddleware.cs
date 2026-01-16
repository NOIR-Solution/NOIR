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
/// 3. After() - (empty, can't determine success here)
/// 4. Finally() - logs completion with duration
///
/// Note: Wolverine middleware cannot directly capture handler return values.
/// Business logic failures (Result.Failure) are detected at the HTTP layer
/// through Serilog request logging which logs 4xx/5xx at WARNING/ERROR level.
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
    /// Called after the handler executes.
    /// Note: This runs before the endpoint converts Result to HTTP response,
    /// so we can't determine actual success/failure here. The HTTP status code
    /// logged by Serilog request logging is the authoritative indicator.
    /// </summary>
    public void After(ILogger<LoggingMiddleware> logger, Envelope envelope)
    {
        // Don't log completion status here - can't detect Result.Failure()
        // HTTP response logging will show the actual status code
    }

    /// <summary>
    /// Called after handler execution completes.
    /// Logs completion with duration - actual success/failure is determined
    /// by Serilog request logging based on HTTP response status codes.
    /// </summary>
    public void Finally(ILogger<LoggingMiddleware> logger, Envelope envelope)
    {
        _stopwatch.Stop();
        var message = envelope.Message;
        var messageType = message?.GetType().Name ?? "Unknown";

        // Log completion - HTTP response status determines actual success/failure
        // Serilog request logging logs 4xx at WARNING, 5xx at ERROR level
        logger.LogInformation(
            "Handled {MessageType} in {ElapsedMs}ms | CorrelationId: {CorrelationId}",
            messageType,
            _stopwatch.ElapsedMilliseconds,
            envelope.CorrelationId);
    }
}
