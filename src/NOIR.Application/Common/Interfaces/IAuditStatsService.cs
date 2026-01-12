namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for computing audit statistics for dashboard display.
/// </summary>
public interface IAuditStatsService
{
    /// <summary>
    /// Gets the current audit statistics for a tenant (or all tenants if null).
    /// </summary>
    Task<AuditStatsUpdate> GetCurrentStatsAsync(string? tenantId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets detailed statistics for a specific date range.
    /// </summary>
    Task<AuditDetailedStats> GetDetailedStatsAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        string? tenantId = null,
        CancellationToken ct = default);
}

/// <summary>
/// Detailed audit statistics with additional breakdowns.
/// </summary>
public record AuditDetailedStats(
    DateTimeOffset FromDate,
    DateTimeOffset ToDate,
    string? TenantId,
    int TotalHttpRequests,
    int TotalHandlerExecutions,
    int TotalEntityChanges,
    int TotalErrors,
    double AvgResponseTimeMs,
    IReadOnlyList<DailyActivitySummary> DailyActivity,
    IReadOnlyList<EntityTypeBreakdown> EntityTypeBreakdown,
    IReadOnlyList<UserActivitySummary> TopUsers,
    IReadOnlyList<HandlerBreakdown> TopHandlers);

/// <summary>
/// Daily activity summary for charts.
/// </summary>
public record DailyActivitySummary(
    DateOnly Date,
    int HttpRequests,
    int EntityChanges,
    int Errors,
    double AvgResponseTimeMs);

/// <summary>
/// Breakdown of activity by entity type.
/// </summary>
public record EntityTypeBreakdown(
    string EntityType,
    int Created,
    int Updated,
    int Deleted,
    int Total);

/// <summary>
/// User activity summary.
/// </summary>
public record UserActivitySummary(
    string? UserId,
    string? UserEmail,
    int RequestCount,
    int ChangeCount);

/// <summary>
/// Handler execution breakdown.
/// </summary>
public record HandlerBreakdown(
    string HandlerName,
    int ExecutionCount,
    int SuccessCount,
    int ErrorCount,
    double AvgDurationMs);
