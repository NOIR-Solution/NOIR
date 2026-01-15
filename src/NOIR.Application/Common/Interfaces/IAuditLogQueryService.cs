using NOIR.Application.Common.Models;
using NOIR.Application.Features.Audit.DTOs;

namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for querying audit log data.
/// Implemented in Infrastructure layer where DbContext is available.
/// </summary>
public interface IAuditLogQueryService
{
    /// <summary>
    /// Gets distinct entity types that have audit history.
    /// </summary>
    Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches for entities that have audit history.
    /// </summary>
    Task<PagedResult<EntitySearchResultDto>> SearchEntitiesAsync(
        string? entityType,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the history timeline for a specific entity.
    /// </summary>
    Task<PagedResult<EntityHistoryEntryDto>> GetEntityHistoryAsync(
        string entityType,
        string entityId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all versions of an entity for comparison.
    /// </summary>
    Task<IReadOnlyList<EntityVersionDto>> GetEntityVersionsAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default);

    #region Activity Timeline

    /// <summary>
    /// Searches the activity timeline with filtering and pagination.
    /// </summary>
    Task<PagedResult<ActivityTimelineEntryDto>> SearchActivityTimelineAsync(
        string? pageContext,
        string? operationType,
        string? userId,
        string? targetId,
        string? correlationId,
        string? searchTerm,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        bool? onlyFailed,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Gets detailed information about a specific activity entry.
    /// </summary>
    Task<ActivityDetailsDto?> GetActivityDetailsAsync(
        Guid handlerAuditLogId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets distinct page contexts that have activity logs.
    /// </summary>
    Task<IReadOnlyList<string>> GetPageContextsAsync(CancellationToken ct = default);

    #endregion
}
