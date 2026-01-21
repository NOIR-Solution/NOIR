namespace NOIR.Infrastructure.Configuration;

/// <summary>
/// Settings for the live configuration management feature.
/// Controls runtime configuration editing, backup retention, and application restart behavior.
/// </summary>
public sealed class ConfigurationManagementSettings
{
    /// <summary>
    /// Enables runtime configuration changes via the admin UI.
    /// IMPORTANT: Set to false in production! Use CI/CD for config changes instead.
    /// </summary>
    public bool EnableRuntimeChanges { get; set; } = false;

    /// <summary>
    /// Number of configuration backups to retain before cleanup.
    /// Backups are timestamped and include the user who made the change.
    /// </summary>
    public int BackupRetentionCount { get; set; } = 5;

    /// <summary>
    /// Maximum time to wait for graceful shutdown when restarting the application (in seconds).
    /// Allows in-flight requests to complete before forcibly terminating.
    /// </summary>
    public int ShutdownTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// List of configuration sections that can be edited via the admin UI.
    /// Sections not in this list will be marked as restricted.
    /// </summary>
    public List<string> AllowedSections { get; set; } = new()
    {
        "DeveloperLogs",
        "Cache",
        "RateLimiting",
        "AuditRetention",
        "Email",
        "JwtSettings"
    };

    /// <summary>
    /// Minimum time interval between application restarts (in minutes).
    /// Prevents rapid restart loops and gives the system time to stabilize.
    /// </summary>
    public int MinimumRestartIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// Configuration sections that require application restart to take effect.
    /// These settings are consumed during startup (middleware, DbContext registration).
    /// Most settings auto-reload via IOptionsMonitor and don't need restart.
    /// </summary>
    public static readonly Dictionary<string, bool> RequiresRestart = new()
    {
        ["JwtSettings"] = false,        // Auto-reload via IOptionsMonitor
        ["DeveloperLogs"] = false,      // Auto-reload
        ["Cache"] = false,              // Auto-reload
        ["RateLimiting"] = false,       // Auto-reload (if using IOptionsMonitor)
        ["Email"] = false,              // Auto-reload
        ["AuditRetention"] = false,     // Auto-reload
        ["ImageProcessing"] = false,    // Auto-reload
        ["ConnectionStrings"] = true,   // RESTART REQUIRED (DbContext)
        ["Kestrel"] = true,             // RESTART REQUIRED (middleware)
        ["Cors"] = true                 // RESTART REQUIRED (middleware)
    };
}
