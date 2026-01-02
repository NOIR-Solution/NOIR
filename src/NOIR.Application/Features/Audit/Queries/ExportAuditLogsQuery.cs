namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Query to export audit logs to CSV format for compliance reporting.
/// </summary>
public sealed record ExportAuditLogsQuery(
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    string? EntityType,
    string? UserId,
    ExportFormat Format = ExportFormat.Csv,
    int MaxRows = 10000) : IAuditableCommand
{
    /// <summary>
    /// Maximum allowed rows for export to prevent memory issues.
    /// </summary>
    public const int MaxAllowedRows = 100000;

    /// <summary>
    /// Maximum allowed date range in days to prevent large exports.
    /// </summary>
    public const int MaxDateRangeDays = 90;

    public object? GetTargetId() => null;
    public AuditOperationType OperationType => AuditOperationType.Query;
}

/// <summary>
/// Export format options.
/// </summary>
public enum ExportFormat
{
    Csv,
    Json
}

/// <summary>
/// Result of an audit export operation.
/// </summary>
public sealed record ExportAuditLogsResult(
    byte[] Data,
    string ContentType,
    string FileName);
