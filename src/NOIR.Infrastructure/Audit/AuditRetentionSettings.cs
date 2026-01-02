namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Configuration settings for audit log retention policy.
/// </summary>
public class AuditRetentionSettings
{
    public const string SectionName = "AuditRetention";

    /// <summary>
    /// Whether the retention job is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Number of days to retain active audit logs before archiving.
    /// Default: 90 days (3 months)
    /// </summary>
    public int ArchiveAfterDays { get; set; } = 90;

    /// <summary>
    /// Number of days to retain archived audit logs before permanent deletion.
    /// Default: 365 days (1 year)
    /// </summary>
    public int DeleteAfterDays { get; set; } = 365;

    /// <summary>
    /// Maximum number of records to process per job run (to avoid long-running transactions).
    /// Default: 10000
    /// </summary>
    public int BatchSize { get; set; } = 10000;

    /// <summary>
    /// Cron expression for job schedule. Default: daily at 2 AM.
    /// </summary>
    public string CronSchedule { get; set; } = "0 2 * * *";

    /// <summary>
    /// Whether to archive records to separate tables before deletion.
    /// If false, records are deleted directly.
    /// Default: true
    /// </summary>
    public bool EnableArchiving { get; set; } = true;

    /// <summary>
    /// Whether to export archived records to file storage before deletion.
    /// Requires IFileStorage to be configured.
    /// Default: false
    /// </summary>
    public bool ExportBeforeDelete { get; set; }

    /// <summary>
    /// Path/prefix for exported archive files.
    /// Default: "audit-archives"
    /// </summary>
    public string ExportPath { get; set; } = "audit-archives";
}
