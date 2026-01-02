namespace NOIR.Application.Features.Audit.DTOs;

/// <summary>
/// Summary view of an HTTP request audit log.
/// </summary>
public sealed record HttpRequestAuditDto(
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
/// Detailed view of an HTTP request audit log with nested handlers and entity changes.
/// </summary>
public sealed record HttpRequestAuditDetailDto(
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
    long? DurationMs,
    IReadOnlyList<HandlerAuditDto> Handlers);

/// <summary>
/// Handler execution audit log.
/// </summary>
public sealed record HandlerAuditDto(
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
    IReadOnlyList<EntityAuditDto> EntityChanges);

/// <summary>
/// Entity change audit log.
/// </summary>
public sealed record EntityAuditDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Operation,
    string? EntityDiff,
    DateTimeOffset Timestamp,
    int Version);

/// <summary>
/// Complete audit trail for a correlation ID.
/// </summary>
public sealed record AuditTrailDto(
    string CorrelationId,
    HttpRequestAuditDetailDto? HttpRequest,
    IReadOnlyList<HandlerAuditDto> Handlers,
    IReadOnlyList<EntityAuditDto> EntityChanges);

/// <summary>
/// History of changes for a specific entity.
/// </summary>
public sealed record EntityHistoryDto(
    string EntityType,
    string EntityId,
    IReadOnlyList<EntityHistoryEntryDto> History);

/// <summary>
/// Single history entry for an entity.
/// </summary>
public sealed record EntityHistoryEntryDto(
    Guid Id,
    string Operation,
    string? EntityDiff,
    DateTimeOffset Timestamp,
    int Version,
    string CorrelationId,
    string? HandlerName,
    string? UserId,
    string? UserEmail);
