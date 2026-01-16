namespace NOIR.Application.Features.DeveloperLogs.DTOs;

/// <summary>
/// Log severity levels matching Serilog's LogEventLevel.
/// Named DevLogLevel to avoid conflicts with Microsoft.Extensions.Logging.LogLevel.
/// </summary>
public enum DevLogLevel
{
    Verbose = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5
}

/// <summary>
/// Data transfer object for a single log entry streamed to clients.
/// </summary>
public sealed record LogEntryDto
{
    /// <summary>
    /// Unique identifier for this log entry.
    /// </summary>
    public required long Id { get; init; }

    /// <summary>
    /// Timestamp when the log was created.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Log severity level.
    /// </summary>
    public required DevLogLevel Level { get; init; }

    /// <summary>
    /// The rendered log message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The original message template (for structured logging).
    /// </summary>
    public string? MessageTemplate { get; init; }

    /// <summary>
    /// The source context (logger name, usually class name).
    /// </summary>
    public string? SourceContext { get; init; }

    /// <summary>
    /// Exception details if this log entry contains an exception.
    /// </summary>
    public ExceptionDto? Exception { get; init; }

    /// <summary>
    /// Additional structured properties.
    /// </summary>
    public IDictionary<string, object?>? Properties { get; init; }

    /// <summary>
    /// HTTP request ID for correlation (if available).
    /// </summary>
    public string? RequestId { get; init; }

    /// <summary>
    /// Trace ID for distributed tracing (if available).
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// User ID associated with this log entry (if available).
    /// </summary>
    public string? UserId { get; init; }

    /// <summary>
    /// Tenant ID associated with this log entry (if available).
    /// </summary>
    public string? TenantId { get; init; }
}

/// <summary>
/// Exception details for log entries.
/// </summary>
public sealed record ExceptionDto
{
    /// <summary>
    /// The exception type name.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The exception message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The stack trace.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Inner exception (if any).
    /// </summary>
    public ExceptionDto? InnerException { get; init; }
}

/// <summary>
/// Response containing the current log level.
/// </summary>
public sealed record LogLevelResponse(string Level, string[] AvailableLevels);

/// <summary>
/// Request to change the log level.
/// </summary>
public sealed record ChangeLogLevelRequest(string Level);

/// <summary>
/// Log level override for a specific source.
/// </summary>
public sealed record LogLevelOverrideDto(string SourcePrefix, string Level);

/// <summary>
/// Response containing all log level overrides.
/// </summary>
public sealed record LogLevelOverridesResponse(
    string GlobalLevel,
    IEnumerable<LogLevelOverrideDto> Overrides);

/// <summary>
/// Paginated response for historical logs.
/// </summary>
public sealed record LogEntriesPagedResponse
{
    /// <summary>
    /// The log entries.
    /// </summary>
    public required IEnumerable<LogEntryDto> Items { get; init; }

    /// <summary>
    /// Total count of matching entries.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public required int Page { get; init; }

    /// <summary>
    /// Page size.
    /// </summary>
    public required int PageSize { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public required int TotalPages { get; init; }

    /// <summary>
    /// Whether there are more pages.
    /// </summary>
    public bool HasMore => Page < TotalPages;
}

/// <summary>
/// Query parameters for searching historical logs.
/// </summary>
public sealed record LogSearchQuery
{
    /// <summary>
    /// Search text (supports regex if prefixed with /).
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Minimum log level to include.
    /// </summary>
    public DevLogLevel? MinLevel { get; init; }

    /// <summary>
    /// Specific log levels to include.
    /// </summary>
    public DevLogLevel[]? Levels { get; init; }

    /// <summary>
    /// Source contexts to include (supports prefix matching).
    /// </summary>
    public string[]? Sources { get; init; }

    /// <summary>
    /// Start of time range.
    /// </summary>
    public DateTimeOffset? From { get; init; }

    /// <summary>
    /// End of time range.
    /// </summary>
    public DateTimeOffset? To { get; init; }

    /// <summary>
    /// Only show entries with exceptions.
    /// </summary>
    public bool? HasException { get; init; }

    /// <summary>
    /// Filter by request ID.
    /// </summary>
    public string? RequestId { get; init; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; init; } = 100;
}

/// <summary>
/// Summary of error patterns found in logs.
/// </summary>
public sealed record ErrorClusterDto
{
    /// <summary>
    /// Unique identifier for this cluster.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Normalized error pattern.
    /// </summary>
    public required string Pattern { get; init; }

    /// <summary>
    /// Number of occurrences.
    /// </summary>
    public required int Count { get; init; }

    /// <summary>
    /// First occurrence.
    /// </summary>
    public required DateTimeOffset FirstSeen { get; init; }

    /// <summary>
    /// Last occurrence.
    /// </summary>
    public required DateTimeOffset LastSeen { get; init; }

    /// <summary>
    /// Sample log entries (max 5).
    /// </summary>
    public required IEnumerable<LogEntryDto> Samples { get; init; }

    /// <summary>
    /// Severity assessment.
    /// </summary>
    public required string Severity { get; init; }
}

/// <summary>
/// Statistics about the log buffer.
/// </summary>
public sealed record LogBufferStatsDto
{
    /// <summary>
    /// Total entries currently in buffer.
    /// </summary>
    public required int TotalEntries { get; init; }

    /// <summary>
    /// Maximum buffer capacity.
    /// </summary>
    public required int MaxCapacity { get; init; }

    /// <summary>
    /// Entries by level.
    /// </summary>
    public required IDictionary<string, int> EntriesByLevel { get; init; }

    /// <summary>
    /// Approximate memory usage in bytes.
    /// </summary>
    public required long MemoryUsageBytes { get; init; }

    /// <summary>
    /// Oldest entry timestamp.
    /// </summary>
    public DateTimeOffset? OldestEntry { get; init; }

    /// <summary>
    /// Newest entry timestamp.
    /// </summary>
    public DateTimeOffset? NewestEntry { get; init; }
}
