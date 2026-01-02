namespace NOIR.Application.Behaviors;

/// <summary>
/// Wolverine middleware that logs warnings for slow handler executions.
/// Threshold is configurable via appsettings.json under "Performance:SlowHandlerThresholdMs".
/// </summary>
/// <remarks>
/// Default threshold is 500ms. Handlers exceeding this threshold will generate
/// a warning log entry, helping identify performance bottlenecks.
///
/// Configuration example in appsettings.json:
/// {
///   "Performance": {
///     "SlowHandlerThresholdMs": 500
///   }
/// }
///
/// Uses Envelope.Message to access the message to avoid Wolverine treating
/// `object` parameter as a service dependency.
/// </remarks>
public class PerformanceMiddleware
{
    private readonly Stopwatch _stopwatch = new();
    private const int DefaultThresholdMs = 500;

    /// <summary>
    /// Called before the handler executes.
    /// Starts the performance timer.
    /// </summary>
    public void Before()
    {
        _stopwatch.Restart();
    }

    /// <summary>
    /// Called after handler execution completes (success or failure).
    /// Logs a warning if execution exceeded the configured threshold.
    /// </summary>
    public void Finally(
        Envelope envelope,
        ILogger<PerformanceMiddleware> logger,
        IConfiguration configuration)
    {
        _stopwatch.Stop();
        var threshold = configuration.GetValue("Performance:SlowHandlerThresholdMs", DefaultThresholdMs);

        if (_stopwatch.ElapsedMilliseconds > threshold)
        {
            var message = envelope.Message;
            logger.LogWarning(
                "SLOW HANDLER: {MessageType} took {ElapsedMs}ms (threshold: {Threshold}ms)",
                message?.GetType().Name ?? "Unknown",
                _stopwatch.ElapsedMilliseconds,
                threshold);
        }
    }
}
