namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Implementation of IAuditStatsService for computing dashboard statistics.
/// Uses optimized queries with caching for performance.
/// </summary>
public class AuditStatsService : IAuditStatsService, IScopedService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AuditStatsService> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public AuditStatsService(
        ApplicationDbContext dbContext,
        IDateTime dateTime,
        IMemoryCache cache,
        ILogger<AuditStatsService> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuditStatsUpdate> GetCurrentStatsAsync(
        string? tenantId = null,
        CancellationToken ct = default)
    {
        var cacheKey = $"audit_stats_{tenantId ?? "all"}";

        if (_cache.TryGetValue<AuditStatsUpdate>(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        var now = _dateTime.UtcNow;
        var todayStart = new DateTimeOffset(now.Date, TimeSpan.Zero);

        // Build base queries with tenant filter
        var httpQuery = _dbContext.HttpRequestAuditLogs
            .AsNoTracking()
            .TagWith("AuditStatsService.GetCurrentStats_Http")
            .Where(h => h.StartTime >= todayStart);

        var handlerQuery = _dbContext.HandlerAuditLogs
            .AsNoTracking()
            .TagWith("AuditStatsService.GetCurrentStats_Handler")
            .Where(h => h.StartTime >= todayStart);

        var entityQuery = _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("AuditStatsService.GetCurrentStats_Entity")
            .Where(e => e.Timestamp >= todayStart);

        if (!string.IsNullOrEmpty(tenantId))
        {
            httpQuery = httpQuery.Where(h => h.TenantId == tenantId);
            handlerQuery = handlerQuery.Where(h => h.TenantId == tenantId);
            entityQuery = entityQuery.Where(e => e.TenantId == tenantId);
        }

        // Execute counts sequentially to avoid DbContext concurrency issues
        // (DbContext is not thread-safe and cannot be used from multiple threads simultaneously)
        var httpCount = await httpQuery.CountAsync(ct);
        var handlerCount = await handlerQuery.CountAsync(ct);
        var entityCount = await entityQuery.CountAsync(ct);
        var errorCount = await handlerQuery.CountAsync(h => !h.IsSuccess, ct);
        var activeUsers = await httpQuery
            .Where(h => h.UserId != null)
            .Select(h => h.UserId)
            .Distinct()
            .CountAsync(ct);
        var avgResponse = await httpQuery
            .Where(h => h.DurationMs.HasValue)
            .AverageAsync(h => (double?)h.DurationMs, ct) ?? 0;

        // Get hourly activity for last 24 hours
        var hourlyActivity = await GetHourlyActivityAsync(tenantId, now, ct);

        var stats = new AuditStatsUpdate(
            now,
            httpCount,
            handlerCount,
            entityCount,
            errorCount,
            activeUsers,
            avgResponse,
            hourlyActivity);

        // Cache the result
        _cache.Set(cacheKey, stats, CacheDuration);

        return stats;
    }

    /// <inheritdoc />
    public async Task<AuditDetailedStats> GetDetailedStatsAsync(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        string? tenantId = null,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Getting detailed stats. From: {From}, To: {To}, Tenant: {Tenant}",
            fromDate, toDate, tenantId);

        // Build base queries
        var httpQuery = _dbContext.HttpRequestAuditLogs
            .AsNoTracking()
            .TagWith("AuditStatsService.GetDetailedStats_Http")
            .Where(h => h.StartTime >= fromDate && h.StartTime <= toDate);

        var handlerQuery = _dbContext.HandlerAuditLogs
            .AsNoTracking()
            .TagWith("AuditStatsService.GetDetailedStats_Handler")
            .Where(h => h.StartTime >= fromDate && h.StartTime <= toDate);

        var entityQuery = _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("AuditStatsService.GetDetailedStats_Entity")
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate);

        if (!string.IsNullOrEmpty(tenantId))
        {
            httpQuery = httpQuery.Where(h => h.TenantId == tenantId);
            handlerQuery = handlerQuery.Where(h => h.TenantId == tenantId);
            entityQuery = entityQuery.Where(e => e.TenantId == tenantId);
        }

        // Total counts
        var totalHttpRequests = await httpQuery.CountAsync(ct);
        var totalHandlerExecutions = await handlerQuery.CountAsync(ct);
        var totalEntityChanges = await entityQuery.CountAsync(ct);
        var totalErrors = await handlerQuery.CountAsync(h => !h.IsSuccess, ct);
        var avgResponseTime = await httpQuery
            .Where(h => h.DurationMs.HasValue)
            .AverageAsync(h => (double?)h.DurationMs, ct) ?? 0;

        // Daily activity
        var dailyActivity = await GetDailyActivityAsync(httpQuery, entityQuery, handlerQuery, ct);

        // Entity type breakdown - fetch grouped data and aggregate client-side
        // EF Core cannot translate conditional aggregates in GroupBy
        var entityGroupedData = await entityQuery
            .GroupBy(e => new { e.EntityType, e.Operation })
            .Select(g => new { g.Key.EntityType, g.Key.Operation, Count = g.Count() })
            .ToListAsync(ct);

        var entityTypeBreakdown = entityGroupedData
            .GroupBy(x => x.EntityType)
            .Select(g => new EntityTypeBreakdown(
                g.Key,
                g.Where(x => x.Operation == "Added").Sum(x => x.Count),
                g.Where(x => x.Operation == "Modified").Sum(x => x.Count),
                g.Where(x => x.Operation == "Deleted").Sum(x => x.Count),
                g.Sum(x => x.Count)))
            .OrderByDescending(e => e.Total)
            .Take(10)
            .ToList();

        // Top users - get request counts first
        var userRequests = await httpQuery
            .Where(h => h.UserId != null)
            .GroupBy(h => new { h.UserId, h.UserEmail })
            .Select(g => new { g.Key.UserId, g.Key.UserEmail, RequestCount = g.Count() })
            .ToListAsync(ct);

        // Get entity change counts per user by joining through CorrelationId
        // EntityAuditLog -> CorrelationId -> HttpRequestAuditLog.UserId
        var userCorrelations = await httpQuery
            .Where(h => h.UserId != null)
            .Select(h => new { h.CorrelationId, h.UserId })
            .Distinct()
            .ToDictionaryAsync(x => x.CorrelationId, x => x.UserId, ct);

        var entityChangesByCorrelation = await entityQuery
            .GroupBy(e => e.CorrelationId)
            .Select(g => new { CorrelationId = g.Key, ChangeCount = g.Count() })
            .ToListAsync(ct);

        // Aggregate entity changes by user
        var userChanges = entityChangesByCorrelation
            .Where(e => userCorrelations.ContainsKey(e.CorrelationId))
            .GroupBy(e => userCorrelations[e.CorrelationId])
            .ToDictionary(g => g.Key!, g => g.Sum(e => e.ChangeCount));

        // Combine into UserActivitySummary with actual entity change counts
        var topUsers = userRequests
            .Select(u => new UserActivitySummary(
                u.UserId,
                u.UserEmail,
                u.RequestCount,
                userChanges.GetValueOrDefault(u.UserId!, 0)))
            .OrderByDescending(u => u.RequestCount)
            .Take(10)
            .ToList();

        // Top handlers - fetch grouped data and aggregate client-side
        // EF Core cannot translate conditional aggregates in GroupBy
        var handlerGroupedData = await handlerQuery
            .GroupBy(h => new { h.HandlerName, h.IsSuccess })
            .Select(g => new {
                g.Key.HandlerName,
                g.Key.IsSuccess,
                Count = g.Count(),
                AvgDuration = g.Average(h => (double?)h.DurationMs) ?? 0
            })
            .ToListAsync(ct);

        var topHandlers = handlerGroupedData
            .GroupBy(x => x.HandlerName)
            .Select(g => new HandlerBreakdown(
                g.Key,
                g.Sum(x => x.Count),
                g.Where(x => x.IsSuccess).Sum(x => x.Count),
                g.Where(x => !x.IsSuccess).Sum(x => x.Count),
                g.Sum(x => x.AvgDuration * x.Count) / Math.Max(g.Sum(x => x.Count), 1)))
            .OrderByDescending(h => h.ExecutionCount)
            .Take(10)
            .ToList();

        return new AuditDetailedStats(
            fromDate,
            toDate,
            tenantId,
            totalHttpRequests,
            totalHandlerExecutions,
            totalEntityChanges,
            totalErrors,
            avgResponseTime,
            dailyActivity,
            entityTypeBreakdown,
            topUsers,
            topHandlers);
    }

    private async Task<IReadOnlyList<HourlyActivityPoint>> GetHourlyActivityAsync(
        string? tenantId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var last24Hours = now.AddHours(-24);

        var httpQuery = _dbContext.HttpRequestAuditLogs
            .AsNoTracking()
            .TagWith("AuditStatsService.GetHourlyActivity_Http")
            .Where(h => h.StartTime >= last24Hours);

        var entityQuery = _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("AuditStatsService.GetHourlyActivity_Entity")
            .Where(e => e.Timestamp >= last24Hours);

        var handlerQuery = _dbContext.HandlerAuditLogs
            .AsNoTracking()
            .TagWith("AuditStatsService.GetHourlyActivity_Handler")
            .Where(h => h.StartTime >= last24Hours && !h.IsSuccess);

        if (!string.IsNullOrEmpty(tenantId))
        {
            httpQuery = httpQuery.Where(h => h.TenantId == tenantId);
            entityQuery = entityQuery.Where(e => e.TenantId == tenantId);
            handlerQuery = handlerQuery.Where(h => h.TenantId == tenantId);
        }

        // Group by hour
        var httpByHour = await httpQuery
            .GroupBy(h => h.StartTime.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Hour, x => x.Count, ct);

        var entityByHour = await entityQuery
            .GroupBy(e => e.Timestamp.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Hour, x => x.Count, ct);

        var errorsByHour = await handlerQuery
            .GroupBy(h => h.StartTime.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Hour, x => x.Count, ct);

        // Build result for all 24 hours
        var result = new List<HourlyActivityPoint>();
        for (var hour = 0; hour < 24; hour++)
        {
            result.Add(new HourlyActivityPoint(
                hour,
                httpByHour.GetValueOrDefault(hour, 0),
                entityByHour.GetValueOrDefault(hour, 0),
                errorsByHour.GetValueOrDefault(hour, 0)));
        }

        return result;
    }

    private async Task<IReadOnlyList<DailyActivitySummary>> GetDailyActivityAsync(
        IQueryable<HttpRequestAuditLog> httpQuery,
        IQueryable<EntityAuditLog> entityQuery,
        IQueryable<HandlerAuditLog> handlerQuery,
        CancellationToken ct)
    {
        // This is a simplified implementation - for production, use raw SQL for better performance
        var httpByDay = await httpQuery
            .GroupBy(h => h.StartTime.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count(),
                AvgDuration = g.Average(h => (double?)h.DurationMs) ?? 0
            })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Date), x => (x.Count, x.AvgDuration), ct);

        var entityByDay = await entityQuery
            .GroupBy(e => e.Timestamp.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Date), x => x.Count, ct);

        var errorsByDay = await handlerQuery
            .Where(h => !h.IsSuccess)
            .GroupBy(h => h.StartTime.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => DateOnly.FromDateTime(x.Date), x => x.Count, ct);

        // Combine all dates
        var allDates = httpByDay.Keys
            .Union(entityByDay.Keys)
            .Union(errorsByDay.Keys)
            .OrderBy(d => d)
            .ToList();

        return allDates.Select(date => new DailyActivitySummary(
            date,
            httpByDay.TryGetValue(date, out var http) ? http.Count : 0,
            entityByDay.GetValueOrDefault(date, 0),
            errorsByDay.GetValueOrDefault(date, 0),
            httpByDay.TryGetValue(date, out var httpData) ? httpData.AvgDuration : 0
        )).ToList();
    }
}
