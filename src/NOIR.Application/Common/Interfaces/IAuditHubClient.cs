namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Strongly-typed SignalR client interface for audit events.
/// Defines all methods that can be invoked on connected clients.
/// </summary>
public interface IAuditHubClient
{
    /// <summary>
    /// Receives an HTTP request audit event.
    /// </summary>
    Task ReceiveHttpRequestAudit(HttpRequestAuditEvent evt);

    /// <summary>
    /// Receives a handler execution audit event.
    /// </summary>
    Task ReceiveHandlerAudit(HandlerAuditEvent evt);

    /// <summary>
    /// Receives an entity change audit event.
    /// </summary>
    Task ReceiveEntityAudit(EntityAuditEvent evt);

    /// <summary>
    /// Receives updated audit statistics for dashboard.
    /// </summary>
    Task ReceiveStatsUpdate(AuditStatsUpdate stats);

    /// <summary>
    /// Receives connection confirmation with initial stats.
    /// </summary>
    Task ReceiveConnectionConfirmed(AuditConnectionInfo info);
}

#region Event DTOs

/// <summary>
/// Real-time HTTP request audit event.
/// </summary>
public record HttpRequestAuditEvent(
    Guid Id,
    string CorrelationId,
    string HttpMethod,
    string Url,
    int? StatusCode,
    string? UserId,
    string? UserEmail,
    string? TenantId,
    string? IpAddress,
    DateTimeOffset Timestamp,
    long? DurationMs,
    int HandlerCount,
    int EntityChangeCount);

/// <summary>
/// Real-time handler execution audit event.
/// </summary>
public record HandlerAuditEvent(
    Guid Id,
    string CorrelationId,
    string HandlerName,
    string OperationType,
    string? TargetDtoType,
    string? TargetDtoId,
    bool IsSuccess,
    string? ErrorMessage,
    DateTimeOffset Timestamp,
    long? DurationMs,
    int EntityChangeCount);

/// <summary>
/// Real-time entity change audit event.
/// </summary>
public record EntityAuditEvent(
    Guid Id,
    string CorrelationId,
    string EntityType,
    string EntityId,
    string Operation,
    DateTimeOffset Timestamp,
    int Version,
    string? ChangeSummary);

/// <summary>
/// Real-time audit statistics update for dashboard.
/// </summary>
public record AuditStatsUpdate(
    DateTimeOffset Timestamp,
    int TodayHttpRequests,
    int TodayHandlerExecutions,
    int TodayEntityChanges,
    int TodayErrors,
    int ActiveUsers,
    double AvgResponseTimeMs,
    IReadOnlyList<HourlyActivityPoint> HourlyActivity);

/// <summary>
/// Single data point for hourly activity chart.
/// </summary>
public record HourlyActivityPoint(
    int Hour,
    int HttpRequests,
    int EntityChanges,
    int Errors);

/// <summary>
/// Connection confirmation with initial state.
/// </summary>
public record AuditConnectionInfo(
    string ConnectionId,
    string? TenantId,
    IReadOnlyList<string> SubscribedGroups,
    AuditStatsUpdate InitialStats);

#endregion
