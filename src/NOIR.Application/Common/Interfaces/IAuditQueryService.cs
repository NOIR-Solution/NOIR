namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Abstracts audit log query operations for handlers in the Application layer.
/// Implementation in Infrastructure layer provides the actual EF Core queries.
/// </summary>
public interface IAuditQueryService
{
    #region Audit Trail

    /// <summary>
    /// Gets the complete audit trail for a correlation ID including HTTP request,
    /// handler executions, and entity changes.
    /// </summary>
    Task<AuditTrailData?> GetAuditTrailAsync(string correlationId, CancellationToken ct = default);

    #endregion

    #region Entity History

    /// <summary>
    /// Gets paginated change history for a specific entity.
    /// </summary>
    Task<(IReadOnlyList<EntityHistoryEntry> Items, int TotalCount)> GetEntityHistoryAsync(
        string entityType,
        string entityId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    #endregion

    #region HTTP Request Audit Logs

    /// <summary>
    /// Gets a queryable for paginated HTTP request audit logs with counts.
    /// </summary>
    IQueryable<HttpRequestAuditData> GetHttpRequestAuditLogsQueryable(
        string? userId = null,
        string? httpMethod = null,
        int? statusCode = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null);

    #endregion

    #region Handler Audit Logs

    /// <summary>
    /// Gets a queryable for paginated handler audit logs with entity changes.
    /// </summary>
    IQueryable<HandlerAuditData> GetHandlerAuditLogsQueryable(
        string? handlerName = null,
        string? operationType = null,
        bool? isSuccess = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null);

    #endregion

    #region Export

    /// <summary>
    /// Gets audit log data for export with the specified filters.
    /// </summary>
    Task<IReadOnlyList<AuditExportData>> GetAuditExportDataAsync(
        int maxRows,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? entityType = null,
        string? userId = null,
        CancellationToken ct = default);

    #endregion
}

#region Data Transfer Objects

/// <summary>
/// Complete audit trail data for a correlation ID.
/// </summary>
public sealed record AuditTrailData(
    string CorrelationId,
    HttpRequestAuditDetail? HttpRequest,
    IReadOnlyList<HandlerAuditItem> Handlers,
    IReadOnlyList<EntityAuditItem> EntityChanges);

/// <summary>
/// HTTP request audit detail with handlers.
/// </summary>
public sealed record HttpRequestAuditDetail(
    Guid Id,
    string CorrelationId,
    string HttpMethod,
    string Url,
    string? QueryString,
    string? RequestHeaders,
    string? RequestBody,
    int? ResponseStatusCode,
    string? ResponseBody,
    string? UserId,
    string? UserEmail,
    string? TenantId,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    long? DurationMs);

/// <summary>
/// Handler audit log item with entity changes.
/// </summary>
public sealed record HandlerAuditItem(
    Guid Id,
    string HandlerName,
    string OperationType,
    string? TargetDtoType,
    string? TargetDtoId,
    string? DtoDiff,
    string? InputParameters,
    string? OutputResult,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    long? DurationMs,
    bool IsSuccess,
    string? ErrorMessage,
    IReadOnlyList<EntityAuditItem> EntityChanges);

/// <summary>
/// Entity change audit item.
/// </summary>
public sealed record EntityAuditItem(
    Guid Id,
    string EntityType,
    string EntityId,
    string Operation,
    string? EntityDiff,
    DateTimeOffset Timestamp,
    int Version);

/// <summary>
/// Entity history entry with context.
/// </summary>
public sealed record EntityHistoryEntry(
    Guid Id,
    string Operation,
    string? EntityDiff,
    DateTimeOffset Timestamp,
    int Version,
    string CorrelationId,
    string? HandlerName,
    string? UserId,
    string? UserEmail);

/// <summary>
/// HTTP request audit data for listing.
/// </summary>
public sealed record HttpRequestAuditData(
    Guid Id,
    string CorrelationId,
    string HttpMethod,
    string Url,
    int? ResponseStatusCode,
    string? UserId,
    string? UserEmail,
    string? TenantId,
    string? IpAddress,
    DateTimeOffset StartTime,
    long? DurationMs,
    int HandlerCount,
    int EntityChangeCount);

/// <summary>
/// Handler audit data for listing.
/// </summary>
public sealed record HandlerAuditData(
    Guid Id,
    string HandlerName,
    string OperationType,
    string? TargetDtoType,
    string? TargetDtoId,
    string? DtoDiff,
    string? InputParameters,
    string? OutputResult,
    DateTimeOffset StartTime,
    DateTimeOffset? EndTime,
    long? DurationMs,
    bool IsSuccess,
    string? ErrorMessage,
    IReadOnlyList<EntityAuditItem> EntityChanges);

/// <summary>
/// Audit export row data.
/// </summary>
public sealed record AuditExportData(
    DateTimeOffset Timestamp,
    string CorrelationId,
    string? TenantId,
    string? UserId,
    string? UserEmail,
    string? IpAddress,
    string EntityType,
    string EntityId,
    string Operation,
    string HandlerName,
    string? EntityDiff,
    int Version);

#endregion
