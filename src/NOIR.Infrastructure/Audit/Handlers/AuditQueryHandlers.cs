using NOIR.Application.Common.Models;
using NOIR.Application.Features.Audit.DTOs;
using NOIR.Application.Features.Audit.Queries;

namespace NOIR.Infrastructure.Audit.Handlers;

/// <summary>
/// Wolverine handler for getting the complete audit trail by correlation ID.
/// </summary>
public class GetAuditTrailQueryHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetAuditTrailQueryHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AuditTrailDto>> Handle(GetAuditTrailQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            return Result.Failure<AuditTrailDto>(Error.Validation("CorrelationId", "CorrelationId is required."));
        }

        // Get HTTP request audit log
        var httpLog = await _dbContext.HttpRequestAuditLogs
            .AsNoTracking()
            .TagWith("GetAuditTrail_HttpRequest")
            .FirstOrDefaultAsync(h => h.CorrelationId == query.CorrelationId, cancellationToken);

        // Get handler audit logs
        var handlerLogs = await _dbContext.HandlerAuditLogs
            .AsNoTracking()
            .TagWith("GetAuditTrail_Handlers")
            .Where(h => h.CorrelationId == query.CorrelationId)
            .OrderBy(h => h.StartTime)
            .ToListAsync(cancellationToken);

        // Get entity audit logs
        var entityLogs = await _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("GetAuditTrail_Entities")
            .Where(e => e.CorrelationId == query.CorrelationId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        // Create lookup for entity to handler mapping (O(1) lookup instead of O(n))
        var entityToHandlerMap = entityLogs.ToDictionary(e => e.Id, e => e.HandlerAuditLogId);

        // Map to DTOs
        var entityDtos = entityLogs.Select(e => new EntityAuditDto(
            e.Id,
            e.EntityType,
            e.EntityId,
            e.Operation,
            e.EntityDiff,
            e.Timestamp,
            e.Version)).ToList();

        // Group entity DTOs by handler ID for efficient lookup
        var entityDtosByHandler = entityDtos
            .Where(e => entityToHandlerMap.TryGetValue(e.Id, out var handlerId) && handlerId.HasValue)
            .GroupBy(e => entityToHandlerMap[e.Id]!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var handlerDtos = handlerLogs.Select(h => new HandlerAuditDto(
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
            entityDtosByHandler.TryGetValue(h.Id, out var entities) ? entities : []
        )).ToList();

        HttpRequestAuditDetailDto? httpDto = null;
        if (httpLog is not null)
        {
            httpDto = new HttpRequestAuditDetailDto(
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
                httpLog.DurationMs,
                handlerDtos);
        }

        var trail = new AuditTrailDto(
            query.CorrelationId,
            httpDto,
            handlerDtos,
            entityDtos);

        return Result.Success(trail);
    }
}

/// <summary>
/// Wolverine handler for getting entity change history.
/// </summary>
public class GetEntityHistoryQueryHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetEntityHistoryQueryHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<EntityHistoryDto>> Handle(GetEntityHistoryQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.EntityType) || string.IsNullOrWhiteSpace(query.EntityId))
        {
            return Result.Failure<EntityHistoryDto>(Error.Validation("EntityType", "EntityType and EntityId are required."));
        }

        var historyQuery = _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("GetEntityHistory")
            .Where(e => e.EntityType == query.EntityType && e.EntityId == query.EntityId)
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

        var count = await historyQuery.CountAsync(cancellationToken);
        var items = await historyQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var historyEntries = items.Select(x => new EntityHistoryEntryDto(
            x.Entity.Id,
            x.Entity.Operation,
            x.Entity.EntityDiff,
            x.Entity.Timestamp,
            x.Entity.Version,
            x.Entity.CorrelationId,
            x.Handler.HandlerName,
            x.HttpRequest?.UserId,
            x.HttpRequest?.UserEmail)).ToList();

        var history = new EntityHistoryDto(
            query.EntityType,
            query.EntityId,
            historyEntries);

        return Result.Success(history);
    }
}

/// <summary>
/// Wolverine handler for getting paginated HTTP request audit logs.
/// </summary>
public class GetHttpRequestAuditLogsQueryHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetHttpRequestAuditLogsQueryHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedList<HttpRequestAuditDto>>> Handle(
        GetHttpRequestAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        var logsQuery = _dbContext.HttpRequestAuditLogs
            .AsNoTracking()
            .TagWith("GetHttpRequestAuditLogs")
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            logsQuery = logsQuery.Where(h => h.UserId == query.UserId);
        }

        if (!string.IsNullOrWhiteSpace(query.HttpMethod))
        {
            logsQuery = logsQuery.Where(h => h.HttpMethod == query.HttpMethod);
        }

        if (query.StatusCode.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.ResponseStatusCode == query.StatusCode.Value);
        }

        if (query.FromDate.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.StartTime >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.StartTime <= query.ToDate.Value);
        }

        // Order by most recent first
        logsQuery = logsQuery.OrderByDescending(h => h.StartTime);

        // Project to DTO with counts using subqueries for efficiency
        // EF Core translates these to SQL subqueries, avoiding N+1
        var dtoQuery = logsQuery.Select(h => new HttpRequestAuditDto(
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

        var result = await PaginatedList<HttpRequestAuditDto>.CreateAsync(
            dtoQuery,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}

/// <summary>
/// Wolverine handler for getting paginated handler audit logs.
/// </summary>
public class GetHandlerAuditLogsQueryHandler
{
    private readonly ApplicationDbContext _dbContext;

    public GetHandlerAuditLogsQueryHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaginatedList<HandlerAuditDto>>> Handle(
        GetHandlerAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        var logsQuery = _dbContext.HandlerAuditLogs
            .AsNoTracking()
            .TagWith("GetHandlerAuditLogs")
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.HandlerName))
        {
            logsQuery = logsQuery.Where(h => h.HandlerName.Contains(query.HandlerName));
        }

        if (!string.IsNullOrWhiteSpace(query.OperationType))
        {
            logsQuery = logsQuery.Where(h => h.OperationType == query.OperationType);
        }

        if (query.IsSuccess.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.IsSuccess == query.IsSuccess.Value);
        }

        if (query.FromDate.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.StartTime >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            logsQuery = logsQuery.Where(h => h.StartTime <= query.ToDate.Value);
        }

        // Order by most recent first
        logsQuery = logsQuery.OrderByDescending(h => h.StartTime);

        // Project to DTO
        var dtoQuery = logsQuery.Select(h => new HandlerAuditDto(
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
            h.EntityAuditLogs.Select(e => new EntityAuditDto(
                e.Id,
                e.EntityType,
                e.EntityId,
                e.Operation,
                e.EntityDiff,
                e.Timestamp,
                e.Version)).ToList()));

        var result = await PaginatedList<HandlerAuditDto>.CreateAsync(
            dtoQuery,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}

/// <summary>
/// Wolverine handler for exporting audit logs to CSV/JSON for compliance reporting.
/// </summary>
public class ExportAuditLogsQueryHandler
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;

    public ExportAuditLogsQueryHandler(ApplicationDbContext dbContext, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
    }

    public async Task<Result<ExportAuditLogsResult>> Handle(
        ExportAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        // VALIDATION: Enforce row limits to prevent memory exhaustion
        var maxRows = Math.Min(query.MaxRows, ExportAuditLogsQuery.MaxAllowedRows);
        if (maxRows <= 0)
        {
            return Result.Failure<ExportAuditLogsResult>(
                Error.Validation("MaxRows", "MaxRows must be greater than 0."));
        }

        // VALIDATION: Enforce date range limits
        if (query.FromDate.HasValue && query.ToDate.HasValue)
        {
            var daysDiff = (query.ToDate.Value - query.FromDate.Value).TotalDays;
            if (daysDiff > ExportAuditLogsQuery.MaxDateRangeDays)
            {
                return Result.Failure<ExportAuditLogsResult>(
                    Error.Validation("DateRange",
                        $"Date range cannot exceed {ExportAuditLogsQuery.MaxDateRangeDays} days. Requested: {daysDiff:N0} days."));
            }
            if (daysDiff < 0)
            {
                return Result.Failure<ExportAuditLogsResult>(
                    Error.Validation("DateRange", "FromDate must be before ToDate."));
            }
        }

        // VALIDATION: Require at least one filter to prevent full table scan
        if (!query.FromDate.HasValue && !query.ToDate.HasValue &&
            string.IsNullOrWhiteSpace(query.EntityType) && string.IsNullOrWhiteSpace(query.UserId))
        {
            return Result.Failure<ExportAuditLogsResult>(
                Error.Validation("Filters", "At least one filter (FromDate, ToDate, EntityType, or UserId) is required."));
        }

        // Build the base query
        var logsQuery = _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("ExportAuditLogs")
            .AsQueryable();

        // Apply filters
        if (query.FromDate.HasValue)
        {
            logsQuery = logsQuery.Where(e => e.Timestamp >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            logsQuery = logsQuery.Where(e => e.Timestamp <= query.ToDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            logsQuery = logsQuery.Where(e => e.EntityType == query.EntityType);
        }

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            logsQuery = logsQuery.Where(e =>
                e.HandlerAuditLog != null &&
                e.HandlerAuditLog.HttpRequestAuditLog != null &&
                e.HandlerAuditLog.HttpRequestAuditLog.UserId == query.UserId);
        }

        // Order by timestamp, apply row limit, and project to DTO in the query
        // This avoids loading full entities with navigation properties into memory
        // EF Core translates the projection to SQL, only fetching needed columns
        var exportData = await logsQuery
            .OrderBy(e => e.Timestamp)
            .Take(maxRows)
            .Select(e => new AuditExportRow
            {
                Timestamp = e.Timestamp,
                CorrelationId = e.CorrelationId,
                TenantId = e.TenantId,
                UserId = e.HandlerAuditLog != null && e.HandlerAuditLog.HttpRequestAuditLog != null
                    ? e.HandlerAuditLog.HttpRequestAuditLog.UserId
                    : null,
                UserEmail = e.HandlerAuditLog != null && e.HandlerAuditLog.HttpRequestAuditLog != null
                    ? e.HandlerAuditLog.HttpRequestAuditLog.UserEmail
                    : null,
                IpAddress = e.HandlerAuditLog != null && e.HandlerAuditLog.HttpRequestAuditLog != null
                    ? e.HandlerAuditLog.HttpRequestAuditLog.IpAddress
                    : null,
                EntityType = e.EntityType,
                EntityId = e.EntityId,
                Operation = e.Operation,
                HandlerName = e.HandlerAuditLog != null ? e.HandlerAuditLog.HandlerName : "Unknown",
                EntityDiff = e.EntityDiff,
                Version = e.Version
            })
            .ToListAsync(cancellationToken);

        // Generate export
        byte[] data;
        string contentType;
        string fileName;

        if (query.Format == ExportFormat.Json)
        {
            data = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(exportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
            contentType = "application/json";
            fileName = $"audit-export-{_dateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        }
        else
        {
            data = GenerateCsv(exportData);
            contentType = "text/csv";
            fileName = $"audit-export-{_dateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        }

        return Result.Success(new ExportAuditLogsResult(data, contentType, fileName));
    }

    private static byte[] GenerateCsv(IReadOnlyList<AuditExportRow> rows)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Timestamp,CorrelationId,TenantId,UserId,UserEmail,IpAddress,EntityType,EntityId,Operation,HandlerName,Version,EntityDiff");

        // Data rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(row.Timestamp.ToString("o")),
                EscapeCsv(row.CorrelationId),
                EscapeCsv(row.TenantId),
                EscapeCsv(row.UserId),
                EscapeCsv(row.UserEmail),
                EscapeCsv(row.IpAddress),
                EscapeCsv(row.EntityType),
                EscapeCsv(row.EntityId),
                EscapeCsv(row.Operation),
                EscapeCsv(row.HandlerName),
                row.Version.ToString(),
                EscapeCsv(row.EntityDiff)));
        }

        // Include UTF-8 BOM for proper encoding detection in Excel and other applications
        var content = Encoding.UTF8.GetBytes(sb.ToString());
        var bom = Encoding.UTF8.GetPreamble();
        var result = new byte[bom.Length + content.Length];
        bom.CopyTo(result, 0);
        content.CopyTo(result, bom.Length);
        return result;
    }

    /// <summary>
    /// Escapes a value for safe CSV output.
    /// Prevents CSV injection attacks (formula execution in Excel).
    /// </summary>
    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        // CRITICAL: Prevent CSV injection (formula execution in Excel/Sheets)
        // Values starting with =, +, -, @, tab, or carriage return can execute formulas
        // Always wrap in quotes and escape internal quotes to prevent formula injection
        var needsPrefix = value.Length > 0 && (
            value[0] == '=' || value[0] == '+' ||
            value[0] == '-' || value[0] == '@' ||
            value[0] == '\t' || value[0] == '\r' ||
            value[0] == '|' || value[0] == '%');

        // Always quote values to ensure safe handling
        // Escape double quotes by doubling them and prefix with single quote if formula-like
        var escaped = value.Replace("\"", "\"\"");
        if (needsPrefix)
        {
            return $"\"'{escaped}\""; // Wrap in quotes with single quote prefix
        }
        return $"\"{escaped}\"";
    }

    private sealed class AuditExportRow
    {
        public DateTimeOffset Timestamp { get; init; }
        public string CorrelationId { get; init; } = default!;
        public string? TenantId { get; init; }
        public string? UserId { get; init; }
        public string? UserEmail { get; init; }
        public string? IpAddress { get; init; }
        public string EntityType { get; init; } = default!;
        public string EntityId { get; init; } = default!;
        public string Operation { get; init; } = default!;
        public string HandlerName { get; init; } = default!;
        public string? EntityDiff { get; init; }
        public int Version { get; init; }
    }
}
