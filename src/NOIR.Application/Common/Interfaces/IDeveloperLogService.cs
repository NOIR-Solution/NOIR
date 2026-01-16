namespace NOIR.Application.Common.Interfaces;

using NOIR.Application.Features.DeveloperLogs.DTOs;

/// <summary>
/// Interface for the log ring buffer that stores log entries in memory.
/// </summary>
public interface ILogRingBuffer
{
    /// <summary>
    /// Adds a log entry to the buffer.
    /// </summary>
    void Add(LogEntryDto entry);

    /// <summary>
    /// Gets the most recent entries from the buffer.
    /// </summary>
    IEnumerable<LogEntryDto> GetRecentEntries(int count);

    /// <summary>
    /// Gets entries before a specific ID (for pagination/history).
    /// </summary>
    IEnumerable<LogEntryDto> GetEntriesBefore(long beforeId, int count);

    /// <summary>
    /// Gets entries matching a filter.
    /// </summary>
    IEnumerable<LogEntryDto> GetFiltered(
        DevLogLevel? minLevel = null,
        string[]? sources = null,
        string? searchPattern = null,
        bool exceptionsOnly = false,
        int maxCount = 1000);

    /// <summary>
    /// Gets error clusters for pattern analysis.
    /// </summary>
    IEnumerable<ErrorClusterDto> GetErrorClusters(int maxClusters = 10);

    /// <summary>
    /// Gets buffer statistics.
    /// </summary>
    LogBufferStatsDto GetStats();

    /// <summary>
    /// Clears all entries from the buffer.
    /// </summary>
    void Clear();

    /// <summary>
    /// Event raised when a new entry is added.
    /// </summary>
    event Action<LogEntryDto>? OnEntryAdded;
}

/// <summary>
/// Interface for managing log levels dynamically.
/// </summary>
public interface ILogLevelService
{
    /// <summary>
    /// Gets the current global minimum log level.
    /// </summary>
    string GetCurrentLevel();

    /// <summary>
    /// Sets the global minimum log level.
    /// </summary>
    bool SetLevel(string level);

    /// <summary>
    /// Gets all available log levels.
    /// </summary>
    string[] GetAvailableLevels();

    /// <summary>
    /// Gets all source-specific level overrides.
    /// </summary>
    IEnumerable<LogLevelOverrideDto> GetOverrides();

    /// <summary>
    /// Sets a level override for a specific source prefix.
    /// </summary>
    bool SetOverride(string sourcePrefix, string level);

    /// <summary>
    /// Removes a level override for a specific source prefix.
    /// </summary>
    bool RemoveOverride(string sourcePrefix);

    /// <summary>
    /// Event raised when the log level changes.
    /// </summary>
    event Action<string>? OnLevelChanged;
}

/// <summary>
/// Interface for accessing historical logs from file storage.
/// </summary>
public interface IHistoricalLogService
{
    /// <summary>
    /// Gets available log file dates.
    /// </summary>
    Task<IEnumerable<DateOnly>> GetAvailableDatesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets log entries for a specific date with filtering.
    /// </summary>
    Task<LogEntriesPagedResponse> GetLogsAsync(
        DateOnly date,
        LogSearchQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Searches logs across multiple dates.
    /// </summary>
    Task<LogEntriesPagedResponse> SearchLogsAsync(
        DateOnly fromDate,
        DateOnly toDate,
        LogSearchQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Gets total file size for a date range.
    /// </summary>
    Task<long> GetLogFileSizeAsync(DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
}

/// <summary>
/// Interface for the SignalR log stream hub context (for sending from services).
/// </summary>
public interface ILogStreamHubContext
{
    /// <summary>
    /// Sends a log entry to all connected clients.
    /// </summary>
    Task SendLogEntryAsync(LogEntryDto entry, CancellationToken ct = default);

    /// <summary>
    /// Sends a batch of log entries to all connected clients.
    /// </summary>
    Task SendLogBatchAsync(IEnumerable<LogEntryDto> entries, CancellationToken ct = default);

    /// <summary>
    /// Notifies all clients that the log level has changed.
    /// </summary>
    Task NotifyLevelChangedAsync(string newLevel, CancellationToken ct = default);

    /// <summary>
    /// Sends buffer statistics to all clients.
    /// </summary>
    Task SendBufferStatsAsync(LogBufferStatsDto stats, CancellationToken ct = default);

    /// <summary>
    /// Gets the number of connected clients.
    /// </summary>
    int GetConnectedClientCount();
}
