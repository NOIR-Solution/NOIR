using System.Text.Json;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Models;
using NOIR.Application.Features.Audit.DTOs;

namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Service for querying audit log data using DbContext directly.
/// Audit log entities extend Entity (not AggregateRoot), so they
/// cannot use the generic repository pattern.
/// </summary>
public class AuditLogQueryService : IAuditLogQueryService, IScopedService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditLogQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetEntityTypesAsync(CancellationToken ct = default)
    {
        return await _dbContext.EntityAuditLogs
            .TagWith("AuditLogQueryService.GetEntityTypes")
            .AsNoTracking()
            .Where(e => !e.IsArchived)
            .Select(e => e.EntityType)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<PagedResult<EntitySearchResultDto>> SearchEntitiesAsync(
        string? entityType,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbContext.EntityAuditLogs
            .TagWith("AuditLogQueryService.SearchEntities")
            .AsNoTracking()
            .Where(e => !e.IsArchived);

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(e => e.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(e =>
                e.EntityId.ToLower().Contains(term) ||
                e.EntityType.ToLower().Contains(term));
        }

        // Group by EntityType + EntityId to get unique entities
        var groupedQuery = query
            .GroupBy(e => new { e.EntityType, e.EntityId })
            .Select(g => new
            {
                g.Key.EntityType,
                g.Key.EntityId,
                LastModified = g.Max(e => e.Timestamp),
                TotalChanges = g.Count()
            });

        var totalCount = await groupedQuery.CountAsync(ct);

        var items = await groupedQuery
            .OrderByDescending(e => e.LastModified)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Get last modifier info for each entity
        var entityKeys = items.Select(i => new { i.EntityType, i.EntityId }).ToList();
        var lastModifiers = await GetLastModifiersAsync(entityKeys, ct);

        var results = items.Select(i =>
        {
            var modifierKey = $"{i.EntityType}:{i.EntityId}";
            lastModifiers.TryGetValue(modifierKey, out var modifierEmail);

            return new EntitySearchResultDto(
                EntityType: i.EntityType,
                EntityId: i.EntityId,
                DisplayName: FormatDisplayName(i.EntityType, i.EntityId),
                Description: null,
                LastModified: i.LastModified,
                LastModifiedBy: modifierEmail,
                TotalChanges: i.TotalChanges);
        }).ToList();

        return PagedResult<EntitySearchResultDto>.Create(results, totalCount, page - 1, pageSize);
    }

    /// <inheritdoc />
    public async Task<PagedResult<EntityHistoryEntryDto>> GetEntityHistoryAsync(
        string entityType,
        string entityId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        string? userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _dbContext.EntityAuditLogs
            .TagWith("AuditLogQueryService.GetEntityHistory")
            .AsNoTracking()
            .Include(e => e.HandlerAuditLog)
            .Where(e => !e.IsArchived)
            .Where(e => e.EntityType == entityType && e.EntityId == entityId);

        if (fromDate.HasValue)
        {
            query = query.Where(e => e.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(e => e.Timestamp <= toDate.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var logs = await query
            .OrderByDescending(e => e.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        // Get user emails for correlation IDs
        var correlationIds = logs.Select(l => l.CorrelationId).Distinct().ToList();
        var userEmails = await GetUserEmailsForCorrelationsAsync(correlationIds, ct);

        // Filter by userId if specified (after we have user info)
        var filteredLogs = logs;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var matchingCorrelations = userEmails
                .Where(kv => kv.Value?.UserId == userId)
                .Select(kv => kv.Key)
                .ToHashSet();
            filteredLogs = logs.Where(l => matchingCorrelations.Contains(l.CorrelationId)).ToList();
        }

        var results = filteredLogs.Select(log =>
        {
            userEmails.TryGetValue(log.CorrelationId, out var userInfo);

            return new EntityHistoryEntryDto(
                Id: log.Id,
                Timestamp: log.Timestamp,
                Operation: log.Operation,
                UserId: userInfo?.UserId,
                UserEmail: userInfo?.Email,
                HandlerName: log.HandlerAuditLog?.HandlerName,
                CorrelationId: log.CorrelationId,
                Changes: ParseEntityDiff(log.EntityDiff),
                Version: log.Version);
        }).ToList();

        return PagedResult<EntityHistoryEntryDto>.Create(results, totalCount, page - 1, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntityVersionDto>> GetEntityVersionsAsync(
        string entityType,
        string entityId,
        CancellationToken ct = default)
    {
        var logs = await _dbContext.EntityAuditLogs
            .TagWith("AuditLogQueryService.GetEntityVersions")
            .AsNoTracking()
            .Include(e => e.HandlerAuditLog)
            .Where(e => !e.IsArchived)
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        // Get user emails for correlation IDs
        var correlationIds = logs.Select(l => l.CorrelationId).Distinct().ToList();
        var userEmails = await GetUserEmailsForCorrelationsAsync(correlationIds, ct);

        // Build cumulative state for each version
        var versions = new List<EntityVersionDto>();
        var currentState = new Dictionary<string, object?>();

        foreach (var log in logs)
        {
            // Apply diff to current state
            ApplyDiffToState(currentState, log.EntityDiff);

            userEmails.TryGetValue(log.CorrelationId, out var userInfo);

            versions.Add(new EntityVersionDto(
                Version: log.Version,
                Timestamp: log.Timestamp,
                Operation: log.Operation,
                UserId: userInfo?.UserId,
                UserEmail: userInfo?.Email,
                State: new Dictionary<string, object?>(currentState)));
        }

        return versions;
    }

    #region Private Helpers

    private async Task<Dictionary<string, string?>> GetLastModifiersAsync(
        IEnumerable<dynamic> entityKeys,
        CancellationToken ct)
    {
        var result = new Dictionary<string, string?>();

        foreach (var key in entityKeys)
        {
            string entityType = key.EntityType;
            string entityId = key.EntityId;

            var lastLog = await _dbContext.EntityAuditLogs
                .TagWith("AuditLogQueryService.GetLastModifier")
                .AsNoTracking()
                .Where(e => e.EntityType == entityType && e.EntityId == entityId && !e.IsArchived)
                .OrderByDescending(e => e.Timestamp)
                .Select(e => e.CorrelationId)
                .FirstOrDefaultAsync(ct);

            if (lastLog != null)
            {
                var httpLog = await _dbContext.HttpRequestAuditLogs
                    .TagWith("AuditLogQueryService.GetLastModifierUser")
                    .AsNoTracking()
                    .Where(h => h.CorrelationId == lastLog)
                    .Select(h => h.UserEmail)
                    .FirstOrDefaultAsync(ct);

                result[$"{entityType}:{entityId}"] = httpLog;
            }
        }

        return result;
    }

    private async Task<Dictionary<string, UserInfo?>> GetUserEmailsForCorrelationsAsync(
        IEnumerable<string> correlationIds,
        CancellationToken ct)
    {
        var result = new Dictionary<string, UserInfo?>();

        var httpLogs = await _dbContext.HttpRequestAuditLogs
            .TagWith("AuditLogQueryService.GetUserEmails")
            .AsNoTracking()
            .Where(h => correlationIds.Contains(h.CorrelationId))
            .Select(h => new { h.CorrelationId, h.UserId, h.UserEmail })
            .ToListAsync(ct);

        foreach (var log in httpLogs)
        {
            result[log.CorrelationId] = new UserInfo(log.UserId, log.UserEmail);
        }

        return result;
    }

    private static IReadOnlyList<FieldChangeDto> ParseEntityDiff(string? entityDiff)
    {
        if (string.IsNullOrWhiteSpace(entityDiff))
            return [];

        try
        {
            // The EntityDiff is stored in format: {"fieldName": {"from": "old", "to": "new"}}
            using var doc = JsonDocument.Parse(entityDiff);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
                return [];

            var changes = new List<FieldChangeDto>();

            foreach (var property in root.EnumerateObject())
            {
                var fieldName = property.Name;
                var diffValue = property.Value;

                if (diffValue.ValueKind == JsonValueKind.Object)
                {
                    // Format: {"from": "old", "to": "new"}
                    object? oldValue = null;
                    object? newValue = null;

                    if (diffValue.TryGetProperty("from", out var fromElement))
                        oldValue = GetJsonValue(fromElement);
                    if (diffValue.TryGetProperty("to", out var toElement))
                        newValue = GetJsonValue(toElement);

                    // Determine operation type
                    var operation = (oldValue, newValue) switch
                    {
                        (null, not null) => ChangeOperation.Added,
                        (not null, null) => ChangeOperation.Removed,
                        _ => ChangeOperation.Modified
                    };

                    changes.Add(new FieldChangeDto(
                        FieldName: fieldName,
                        OldValue: oldValue,
                        NewValue: newValue,
                        Operation: operation
                    ));
                }
            }

            return changes;
        }
        catch
        {
            return [];
        }
    }

    private static object? GetJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.GetRawText(),
            JsonValueKind.Object => element.GetRawText(),
            _ => element.GetRawText()
        };
    }

    private static string ExtractFieldName(string path)
    {
        // Remove leading slash and convert JSON pointer to property name
        // e.g., "/Name" -> "Name", "/Address/Street" -> "Address.Street"
        return path.TrimStart('/').Replace("/", ".");
    }

    private static ChangeOperation MapOperation(string op)
    {
        return op.ToLower() switch
        {
            "add" => ChangeOperation.Added,
            "remove" => ChangeOperation.Removed,
            "replace" => ChangeOperation.Modified,
            _ => ChangeOperation.Modified
        };
    }

    private static void ApplyDiffToState(Dictionary<string, object?> state, string? entityDiff)
    {
        if (string.IsNullOrWhiteSpace(entityDiff))
            return;

        try
        {
            var patches = JsonSerializer.Deserialize<List<JsonPatchOperation>>(entityDiff);
            if (patches == null)
                return;

            foreach (var patch in patches)
            {
                var fieldName = ExtractFieldName(patch.Path);

                switch (patch.Op.ToLower())
                {
                    case "add":
                    case "replace":
                        state[fieldName] = patch.Value;
                        break;
                    case "remove":
                        state.Remove(fieldName);
                        break;
                }
            }
        }
        catch
        {
            // Ignore parse errors
        }
    }

    private static string FormatDisplayName(string entityType, string entityId)
    {
        // Create a human-readable display name
        return $"{entityType} ({entityId})";
    }

    #endregion

    #region Activity Timeline

    /// <inheritdoc />
    public async Task<PagedResult<ActivityTimelineEntryDto>> SearchActivityTimelineAsync(
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
        CancellationToken ct = default)
    {
        var query = _dbContext.HandlerAuditLogs
            .TagWith("AuditLogQueryService.SearchActivityTimeline")
            .AsNoTracking()
            .Include(h => h.HttpRequestAuditLog)
            .Where(h => !h.IsArchived)
            // Only show activities with page context (UI-triggered actions)
            .Where(h => h.PageContext != null);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(pageContext))
        {
            query = query.Where(h => h.PageContext == pageContext);
        }

        if (!string.IsNullOrWhiteSpace(operationType))
        {
            query = query.Where(h => h.OperationType == operationType);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(h => h.StartTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(h => h.StartTime <= toDate.Value);
        }

        if (onlyFailed == true)
        {
            query = query.Where(h => !h.IsSuccess);
        }

        // Enhanced unified search - searches across multiple fields
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(h =>
                // Display fields
                (h.TargetDisplayName != null && h.TargetDisplayName.ToLower().Contains(term)) ||
                (h.ActionDescription != null && h.ActionDescription.ToLower().Contains(term)) ||
                h.HandlerName.ToLower().Contains(term) ||
                // IDs - exact or partial match
                (h.TargetDtoId != null && h.TargetDtoId.ToLower().Contains(term)) ||
                (h.CorrelationId != null && h.CorrelationId.ToLower().Contains(term)) ||
                // Context
                (h.PageContext != null && h.PageContext.ToLower().Contains(term)) ||
                // HTTP request details
                (h.HttpRequestAuditLog != null && h.HttpRequestAuditLog.Url.ToLower().Contains(term)) ||
                (h.HttpRequestAuditLog != null && h.HttpRequestAuditLog.HttpMethod.ToLower().Contains(term)) ||
                (h.HttpRequestAuditLog != null && h.HttpRequestAuditLog.UserEmail != null && h.HttpRequestAuditLog.UserEmail.ToLower().Contains(term)) ||
                // DTO diff content (JSON search)
                (h.DtoDiff != null && h.DtoDiff.ToLower().Contains(term)) ||
                // Input/output JSON
                (h.InputParameters != null && h.InputParameters.ToLower().Contains(term)) ||
                // Entity changes - search field names and values
                h.EntityAuditLogs.Any(e =>
                    e.EntityDiff != null && e.EntityDiff.ToLower().Contains(term)));
        }

        // Filter by user if specified
        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(h => h.HttpRequestAuditLog != null && h.HttpRequestAuditLog.UserId == userId);
        }

        // Filter by target entity ID (e.g., track all changes to a specific user)
        if (!string.IsNullOrWhiteSpace(targetId))
        {
            query = query.Where(h => h.TargetDtoId == targetId);
        }

        // Filter by correlation ID (same HTTP request)
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            query = query.Where(h => h.CorrelationId == correlationId);
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(h => h.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new
            {
                h.Id,
                h.StartTime,
                UserEmail = h.HttpRequestAuditLog != null ? h.HttpRequestAuditLog.UserEmail : null,
                UserId = h.HttpRequestAuditLog != null ? h.HttpRequestAuditLog.UserId : null,
                h.PageContext,
                h.OperationType,
                h.ActionDescription,
                h.TargetDisplayName,
                h.TargetDtoType,
                h.TargetDtoId,
                h.IsSuccess,
                h.DurationMs,
                EntityChangeCount = h.EntityAuditLogs.Count,
                h.CorrelationId,
                h.HandlerName
            })
            .ToListAsync(ct);

        var results = items.Select(h => new ActivityTimelineEntryDto(
            Id: h.Id,
            Timestamp: h.StartTime,
            UserEmail: h.UserEmail,
            UserId: h.UserId,
            DisplayContext: h.PageContext!, // Always non-null due to filter
            OperationType: h.OperationType,
            ActionDescription: h.ActionDescription,
            TargetDisplayName: h.TargetDisplayName,
            TargetDtoType: h.TargetDtoType,
            TargetDtoId: h.TargetDtoId,
            IsSuccess: h.IsSuccess,
            DurationMs: h.DurationMs,
            EntityChangeCount: h.EntityChangeCount,
            CorrelationId: h.CorrelationId,
            HandlerName: h.HandlerName
        )).ToList();

        return PagedResult<ActivityTimelineEntryDto>.Create(results, totalCount, page - 1, pageSize);
    }

    /// <inheritdoc />
    public async Task<ActivityDetailsDto?> GetActivityDetailsAsync(
        Guid handlerAuditLogId,
        CancellationToken ct = default)
    {
        var handler = await _dbContext.HandlerAuditLogs
            .TagWith("AuditLogQueryService.GetActivityDetails")
            .AsNoTracking()
            .Include(h => h.HttpRequestAuditLog)
            .Include(h => h.EntityAuditLogs)
            .FirstOrDefaultAsync(h => h.Id == handlerAuditLogId, ct);

        if (handler is null)
        {
            return null;
        }

        // Build timeline entry
        var entry = new ActivityTimelineEntryDto(
            Id: handler.Id,
            Timestamp: handler.StartTime,
            UserEmail: handler.HttpRequestAuditLog?.UserEmail,
            UserId: handler.HttpRequestAuditLog?.UserId,
            DisplayContext: handler.PageContext ?? handler.HandlerName,
            OperationType: handler.OperationType,
            ActionDescription: handler.ActionDescription,
            TargetDisplayName: handler.TargetDisplayName,
            TargetDtoType: handler.TargetDtoType,
            TargetDtoId: handler.TargetDtoId,
            IsSuccess: handler.IsSuccess,
            DurationMs: handler.DurationMs,
            EntityChangeCount: handler.EntityAuditLogs.Count,
            CorrelationId: handler.CorrelationId,
            HandlerName: handler.HandlerName
        );

        // Build HTTP request details
        HttpRequestDetailsDto? httpRequest = null;
        if (handler.HttpRequestAuditLog is not null)
        {
            var http = handler.HttpRequestAuditLog;
            httpRequest = new HttpRequestDetailsDto(
                Id: http.Id,
                Method: http.HttpMethod,
                Path: http.Url,
                StatusCode: http.ResponseStatusCode ?? 0,
                QueryString: http.QueryString,
                ClientIpAddress: http.IpAddress,
                UserAgent: http.UserAgent,
                RequestTime: http.StartTime,
                DurationMs: http.DurationMs
            );
        }

        // Build entity changes
        var entityChanges = handler.EntityAuditLogs
            .OrderBy(e => e.Timestamp)
            .Select(e => new EntityChangeDto(
                Id: e.Id,
                EntityType: e.EntityType,
                EntityId: e.EntityId,
                Operation: e.Operation,
                Version: e.Version,
                Timestamp: e.Timestamp,
                Changes: ParseEntityDiff(e.EntityDiff)
            ))
            .ToList();

        return new ActivityDetailsDto(
            Entry: entry,
            InputParameters: handler.InputParameters,
            OutputResult: handler.OutputResult,
            DtoDiff: handler.DtoDiff,
            ErrorMessage: handler.ErrorMessage,
            HttpRequest: httpRequest,
            EntityChanges: entityChanges
        );
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetPageContextsAsync(CancellationToken ct = default)
    {
        return await _dbContext.HandlerAuditLogs
            .TagWith("AuditLogQueryService.GetPageContexts")
            .AsNoTracking()
            .Where(h => !h.IsArchived && h.PageContext != null)
            .Select(h => h.PageContext!)
            .Distinct()
            .OrderBy(p => p)
            .ToListAsync(ct);
    }

    #endregion

    #region Helper Types

    private sealed record UserInfo(string? UserId, string? Email);

    private sealed class JsonPatchOperation
    {
        public string Op { get; set; } = "";
        public string Path { get; set; } = "";
        public object? Value { get; set; }
        public object? OldValue { get; set; }
    }

    #endregion
}
