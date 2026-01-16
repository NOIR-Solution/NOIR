namespace NOIR.Infrastructure.Logging;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration settings for the developer log system.
/// </summary>
public sealed class DeveloperLogSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "DeveloperLogs";

    /// <summary>
    /// Whether the developer log system is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum number of log entries to keep in memory.
    /// Default: 50000 entries (~50MB estimated).
    /// </summary>
    [Range(1000, 500000)]
    public int BufferCapacity { get; set; } = 50000;

    /// <summary>
    /// Path for log files (relative to application root).
    /// </summary>
    public string LogFilePath { get; set; } = "logs/noir-.json";

    /// <summary>
    /// Number of days to retain log files.
    /// </summary>
    [Range(1, 365)]
    public int RetainedFileCountLimit { get; set; } = 90;

    /// <summary>
    /// Maximum log file size in MB before rolling.
    /// </summary>
    [Range(1, 1024)]
    public int FileSizeLimitMb { get; set; } = 100;

    /// <summary>
    /// Whether to enable file logging.
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Whether to enable real-time SignalR streaming.
    /// </summary>
    public bool EnableRealTimeStreaming { get; set; } = true;

    /// <summary>
    /// Batch size for sending logs to connected clients.
    /// </summary>
    [Range(1, 100)]
    public int StreamBatchSize { get; set; } = 10;

    /// <summary>
    /// Interval in milliseconds to flush log batches to clients.
    /// </summary>
    [Range(10, 1000)]
    public int StreamFlushIntervalMs { get; set; } = 50;

    /// <summary>
    /// Maximum entries per second to stream (rate limiting).
    /// </summary>
    [Range(100, 10000)]
    public int MaxEntriesPerSecond { get; set; } = 1000;

    /// <summary>
    /// Maximum size of a single log entry in bytes.
    /// </summary>
    [Range(1024, 1048576)]
    public int MaxEntrySize { get; set; } = 65536; // 64KB

    /// <summary>
    /// Whether to mask sensitive data in logs.
    /// </summary>
    public bool MaskSensitiveData { get; set; } = true;

    /// <summary>
    /// Regex patterns to mask in log messages.
    /// </summary>
    public string[] SensitivePatterns { get; set; } =
    [
        @"password['""]?\s*[:=]\s*['""]?[^'""]+",
        @"bearer\s+[a-zA-Z0-9\-_]+\.[a-zA-Z0-9\-_]+\.[a-zA-Z0-9\-_]+",
        @"api[_-]?key['""]?\s*[:=]\s*['""]?[^'""\s]+",
        @"secret['""]?\s*[:=]\s*['""]?[^'""\s]+",
        @"token['""]?\s*[:=]\s*['""]?[^'""\s]+"
    ];

    /// <summary>
    /// Default minimum log level for the developer log system.
    /// </summary>
    public string DefaultMinimumLevel { get; set; } = "Information";

    /// <summary>
    /// Source-specific log level overrides.
    /// </summary>
    public Dictionary<string, string> LevelOverrides { get; set; } = new()
    {
        ["Microsoft"] = "Warning",
        ["Microsoft.EntityFrameworkCore"] = "Warning",
        ["Hangfire"] = "Warning",
        ["System"] = "Warning"
    };
}
