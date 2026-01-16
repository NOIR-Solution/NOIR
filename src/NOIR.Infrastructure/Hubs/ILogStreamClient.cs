namespace NOIR.Infrastructure.Hubs;

using NOIR.Application.Features.DeveloperLogs.DTOs;

/// <summary>
/// Strongly-typed SignalR client interface for log streaming.
/// Defines methods that can be called on connected clients.
/// </summary>
public interface ILogStreamClient
{
    /// <summary>
    /// Receives a single log entry.
    /// </summary>
    Task ReceiveLogEntry(LogEntryDto entry);

    /// <summary>
    /// Receives a batch of log entries (for efficiency).
    /// </summary>
    Task ReceiveLogBatch(IEnumerable<LogEntryDto> entries);

    /// <summary>
    /// Notifies clients that the global log level has changed.
    /// </summary>
    Task ReceiveLevelChanged(string newLevel);

    /// <summary>
    /// Receives buffer statistics update.
    /// </summary>
    Task ReceiveBufferStats(LogBufferStatsDto stats);

    /// <summary>
    /// Receives error summary update.
    /// </summary>
    Task ReceiveErrorSummary(IEnumerable<ErrorClusterDto> clusters);
}
