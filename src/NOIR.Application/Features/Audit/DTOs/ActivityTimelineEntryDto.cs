namespace NOIR.Application.Features.Audit.DTOs;

/// <summary>
/// Represents a single entry in the activity timeline.
/// </summary>
public sealed record ActivityTimelineEntryDto(
    /// <summary>
    /// Unique identifier for the audit entry.
    /// </summary>
    Guid Id,

    /// <summary>
    /// When the action occurred.
    /// </summary>
    DateTimeOffset Timestamp,

    /// <summary>
    /// User who performed the action.
    /// </summary>
    string? UserEmail,

    /// <summary>
    /// User ID who performed the action.
    /// </summary>
    string? UserId,

    /// <summary>
    /// Page context (e.g., "Users", "Tenants") or handler name as fallback.
    /// </summary>
    string DisplayContext,

    /// <summary>
    /// Type of operation: Create, Update, Delete.
    /// </summary>
    string OperationType,

    /// <summary>
    /// Human-readable description (e.g., "edited User John Doe").
    /// </summary>
    string? ActionDescription,

    /// <summary>
    /// Display name of the target entity.
    /// </summary>
    string? TargetDisplayName,

    /// <summary>
    /// Type of target DTO (e.g., "UserDto", "TenantDto").
    /// </summary>
    string? TargetDtoType,

    /// <summary>
    /// ID of the target entity.
    /// </summary>
    string? TargetDtoId,

    /// <summary>
    /// Whether the action succeeded.
    /// </summary>
    bool IsSuccess,

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    long? DurationMs,

    /// <summary>
    /// Number of entity changes made by this action.
    /// </summary>
    int EntityChangeCount,

    /// <summary>
    /// Correlation ID linking all audit entries from the same HTTP request.
    /// </summary>
    string? CorrelationId,

    /// <summary>
    /// Name of the handler (Command/Query) that triggered this action.
    /// </summary>
    string? HandlerName
);

/// <summary>
/// Detailed view of an activity entry with all related data.
/// </summary>
public sealed record ActivityDetailsDto(
    /// <summary>
    /// Basic timeline entry information.
    /// </summary>
    ActivityTimelineEntryDto Entry,

    /// <summary>
    /// Input parameters (command/DTO) as JSON.
    /// </summary>
    string? InputParameters,

    /// <summary>
    /// Output result as JSON.
    /// </summary>
    string? OutputResult,

    /// <summary>
    /// DTO changes as JSON Patch diff.
    /// </summary>
    string? DtoDiff,

    /// <summary>
    /// Error message if the action failed.
    /// </summary>
    string? ErrorMessage,

    /// <summary>
    /// HTTP request details.
    /// </summary>
    HttpRequestDetailsDto? HttpRequest,

    /// <summary>
    /// Entity changes made by this action.
    /// </summary>
    IReadOnlyList<EntityChangeDto> EntityChanges
);

/// <summary>
/// HTTP request details for audit drill-down.
/// </summary>
public sealed record HttpRequestDetailsDto(
    Guid Id,
    string Method,
    string Path,
    int StatusCode,
    string? QueryString,
    string? ClientIpAddress,
    string? UserAgent,
    DateTimeOffset RequestTime,
    long? DurationMs
);

/// <summary>
/// Entity change details for audit drill-down.
/// </summary>
public sealed record EntityChangeDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Operation,
    int Version,
    DateTimeOffset Timestamp,
    IReadOnlyList<FieldChangeDto> Changes
);
