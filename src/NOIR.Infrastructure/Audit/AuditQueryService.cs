namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Implementation of IAuditQueryService that uses EF Core for audit log queries.
/// Provides optimized queries with proper tagging for debugging.
/// </summary>
public class AuditQueryService : IAuditQueryService, IScopedService
{
    private readonly ApplicationDbContext _dbContext;

    public AuditQueryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    #region Audit Trail

    public async Task<AuditTrailData?> GetAuditTrailAsync(string correlationId, CancellationToken ct = default)
    {
        // Get HTTP request audit log
        var httpLog = await _dbContext.HttpRequestAuditLogs
            .AsNoTracking()
            .TagWith("GetAuditTrail_HttpRequest")
            .FirstOrDefaultAsync(h => h.CorrelationId == correlationId, ct);

        // Get handler audit logs
        var handlerLogs = await _dbContext.HandlerAuditLogs
            .AsNoTracking()
            .TagWith("GetAuditTrail_Handlers")
            .Where(h => h.CorrelationId == correlationId)
            .OrderBy(h => h.StartTime)
            .ToListAsync(ct);

        // Get entity audit logs
        var entityLogs = await _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("GetAuditTrail_Entities")
            .Where(e => e.CorrelationId == correlationId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(ct);

        // Create lookup for entity to handler mapping
        var entityToHandlerMap = entityLogs.ToDictionary(e => e.Id, e => e.HandlerAuditLogId);

        // Map to data records
        var entityItems = entityLogs.Select(e => new EntityAuditItem(
            e.Id,
            e.EntityType,
            e.EntityId,
            e.Operation,
            e.EntityDiff,
            e.Timestamp,
            e.Version)).ToList();

        // Group entity items by handler ID
        var entityItemsByHandler = entityItems
            .Where(e => entityToHandlerMap.TryGetValue(e.Id, out var handlerId) && handlerId.HasValue)
            .GroupBy(e => entityToHandlerMap[e.Id]!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var handlerItems = handlerLogs.Select(h => new HandlerAuditItem(
            h.Id,
            h.HandlerName,
            h.OperationType,
            h.TargetDtoType,
            h.TargetDtoId,
            h.DtoDiff,
            h.InputParameters,
            h.OutputResult,
            h.StartTime,
            h.EndTime,
            h.DurationMs,
            h.IsSuccess,
            h.ErrorMessage,
            entityItemsByHandler.TryGetValue(h.Id, out var entities) ? entities : []
        )).ToList();

        HttpRequestAuditDetail? httpDetail = null;
        if (httpLog is not null)
        {
            httpDetail = new HttpRequestAuditDetail(
                httpLog.Id,
                httpLog.CorrelationId,
                httpLog.HttpMethod,
                httpLog.Url,
                httpLog.QueryString,
                httpLog.RequestHeaders,
                httpLog.RequestBody,
                httpLog.ResponseStatusCode,
                httpLog.ResponseBody,
                httpLog.UserId,
                httpLog.UserEmail,
                httpLog.TenantId,
                httpLog.IpAddress,
                httpLog.UserAgent,
                httpLog.StartTime,
                httpLog.EndTime,
                httpLog.DurationMs);
        }

        return new AuditTrailData(
            correlationId,
            httpDetail,
            handlerItems,
            entityItems);
    }

    #endregion

    #region Entity History

    public async Task<(IReadOnlyList<EntityHistoryEntry> Items, int TotalCount)> GetEntityHistoryAsync(
        string entityType,
        string entityId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var historyQuery = _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("GetEntityHistory")
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.Timestamp)
            .Join(
                _dbContext.HandlerAuditLogs.AsNoTracking(),
                e => e.HandlerAuditLogId,
                h => h.Id,
                (e, h) => new { Entity = e, Handler = h })
            .GroupJoin(
                _dbContext.HttpRequestAuditLogs.AsNoTracking(),
                eh => eh.Handler.HttpRequestAuditLogId,
                http => http.Id,
                (eh, httpGroup) => new { eh.Entity, eh.Handler, HttpRequest = httpGroup.FirstOrDefault() });

        var count = await historyQuery.CountAsync(ct);
        var items = await historyQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var entries = items.Select(x => new EntityHistoryEntry(
            x.Entity.Id,
            x.Entity.Operation,
            x.Entity.EntityDiff,
            x.Entity.Timestamp,
            x.Entity.Version,
            x.Entity.CorrelationId,
            x.Handler.HandlerName,
            x.HttpRequest?.UserId,
            x.HttpRequest?.UserEmail)).ToList();

        return (entries, count);
    }

    #endregion

    #region HTTP Request Audit Logs

    public IQueryable<HttpRequestAuditData> GetHttpRequestAuditLogsQueryable(
        string? userId = null,
        string? httpMethod = null,
        int? statusCode = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var logsQuery = _dbContext.HttpRequestAuditLogs
            .AsNoTracking()
            .TagWith("GetHttpRequestAuditLogs")
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(userId))
        {
            logsQuery = logsQuery.Where(h => h.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(httpMethod))
        {
            logsQuery = logsQuery.Where(h => h.HttpMethod == httpMethod);
        }

        if (statusCode.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.ResponseStatusCode == statusCode.Value);
        }

        if (fromDate.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.StartTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.StartTime <= toDate.Value);
        }

        // Order and project
        return logsQuery
            .OrderByDescending(h => h.StartTime)
            .Select(h => new HttpRequestAuditData(
                h.Id,
                h.CorrelationId,
                h.HttpMethod,
                h.Url,
                h.ResponseStatusCode,
                h.UserId,
                h.UserEmail,
                h.TenantId,
                h.IpAddress,
                h.StartTime,
                h.DurationMs,
                _dbContext.HandlerAuditLogs.Count(ha => ha.HttpRequestAuditLogId == h.Id),
                _dbContext.EntityAuditLogs.Count(e =>
                    _dbContext.HandlerAuditLogs.Any(ha =>
                        ha.HttpRequestAuditLogId == h.Id && ha.Id == e.HandlerAuditLogId))));
    }

    #endregion

    #region Handler Audit Logs

    public IQueryable<HandlerAuditData> GetHandlerAuditLogsQueryable(
        string? handlerName = null,
        string? operationType = null,
        bool? isSuccess = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null)
    {
        var logsQuery = _dbContext.HandlerAuditLogs
            .AsNoTracking()
            .TagWith("GetHandlerAuditLogs")
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(handlerName))
        {
            logsQuery = logsQuery.Where(h => h.HandlerName.Contains(handlerName));
        }

        if (!string.IsNullOrWhiteSpace(operationType))
        {
            logsQuery = logsQuery.Where(h => h.OperationType == operationType);
        }

        if (isSuccess.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.IsSuccess == isSuccess.Value);
        }

        if (fromDate.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.StartTime >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.StartTime <= toDate.Value);
        }

        // Order and project
        return logsQuery
            .OrderByDescending(h => h.StartTime)
            .Select(h => new HandlerAuditData(
                h.Id,
                h.HandlerName,
                h.OperationType,
                h.TargetDtoType,
                h.TargetDtoId,
                h.DtoDiff,
                h.InputParameters,
                h.OutputResult,
                h.StartTime,
                h.EndTime,
                h.DurationMs,
                h.IsSuccess,
                h.ErrorMessage,
                h.EntityAuditLogs.Select(e => new EntityAuditItem(
                    e.Id,
                    e.EntityType,
                    e.EntityId,
                    e.Operation,
                    e.EntityDiff,
                    e.Timestamp,
                    e.Version)).ToList()));
    }

    #endregion

    #region Export

    public async Task<IReadOnlyList<AuditExportData>> GetAuditExportDataAsync(
        int maxRows,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        string? entityType = null,
        string? userId = null,
        CancellationToken ct = default)
    {
        var logsQuery = _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("ExportAuditLogs")
            .AsQueryable();

        // Apply filters
        if (fromDate.HasValue)
        {
            logsQuery = logsQuery.Where(e => e.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            logsQuery = logsQuery.Where(e => e.Timestamp <= toDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            logsQuery = logsQuery.Where(e => e.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(userId))
        {
            logsQuery = logsQuery.Where(e =>
                e.HandlerAuditLog != null &&
                e.HandlerAuditLog.HttpRequestAuditLog != null &&
                e.HandlerAuditLog.HttpRequestAuditLog.UserId == userId);
        }

        return await logsQuery
            .OrderBy(e => e.Timestamp)
            .Take(maxRows)
            .Select(e => new AuditExportData(
                e.Timestamp,
                e.CorrelationId,
                e.TenantId,
                e.HandlerAuditLog != null && e.HandlerAuditLog.HttpRequestAuditLog != null
                    ? e.HandlerAuditLog.HttpRequestAuditLog.UserId
                    : null,
                e.HandlerAuditLog != null && e.HandlerAuditLog.HttpRequestAuditLog != null
                    ? e.HandlerAuditLog.HttpRequestAuditLog.UserEmail
                    : null,
                e.HandlerAuditLog != null && e.HandlerAuditLog.HttpRequestAuditLog != null
                    ? e.HandlerAuditLog.HttpRequestAuditLog.IpAddress
                    : null,
                e.EntityType,
                e.EntityId,
                e.Operation,
                e.HandlerAuditLog != null ? e.HandlerAuditLog.HandlerName : "Unknown",
                e.EntityDiff,
                e.Version))
            .ToListAsync(ct);
    }

    #endregion
}
