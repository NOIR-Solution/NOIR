namespace NOIR.Infrastructure.Logging;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Wrapper around SignalR IHubContext for sending log entries from services.
/// Registered as singleton since IHubContext is singleton and this holds no per-request state.
/// Required for deferred SignalR sink initialization during app startup.
/// </summary>
public sealed class LogStreamHubContext : ILogStreamHubContext, ISingletonService
{
    private readonly IHubContext<LogStreamHub, ILogStreamClient> _hubContext;
    private readonly ILogger<LogStreamHubContext> _logger;

    public LogStreamHubContext(
        IHubContext<LogStreamHub, ILogStreamClient> hubContext,
        ILogger<LogStreamHubContext> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendLogEntryAsync(LogEntryDto entry, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group("log_stream").ReceiveLogEntry(entry);
        }
        catch (Exception ex)
        {
            // Log to trace - don't use ILogger recursively for log sink errors
            System.Diagnostics.Trace.WriteLine($"[LogStreamHubContext] Failed to send log entry: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task SendLogBatchAsync(IEnumerable<LogEntryDto> entries, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group("log_stream").ReceiveLogBatch(entries);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LogStreamHubContext] Failed to send log batch: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task NotifyLevelChangedAsync(string newLevel, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group("log_stream").ReceiveLevelChanged(newLevel);
            _logger.LogDebug("Notified clients of log level change to {NewLevel}", newLevel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify clients of log level change");
        }
    }

    /// <inheritdoc />
    public async Task SendBufferStatsAsync(LogBufferStatsDto stats, CancellationToken ct = default)
    {
        try
        {
            await _hubContext.Clients.Group("log_stream").ReceiveBufferStats(stats);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[LogStreamHubContext] Failed to send buffer stats: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public int GetConnectedClientCount()
    {
        return LogStreamHub.GetConnectionCount();
    }
}
