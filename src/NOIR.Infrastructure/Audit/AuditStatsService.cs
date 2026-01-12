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

        // Execute counts in parallel
        var httpCountTask = httpQuery.CountAsync(ct);
        var handlerCountTask = handlerQuery.CountAsync(ct);
        var entityCountTask = entityQuery.CountAsync(ct);
        var errorCountTask = handlerQuery.CountAsync(h => !h.IsSuccess, ct);
        var activeUsersTask = httpQuery
            .Where(h => h.UserId != null)
            .Select(h => h.UserId)
            .Distinct()
            .CountAsync(ct);
        var avgResponseTask = httpQuery
            .Where(h => h.DurationMs.HasValue)
            .AverageAsync(h => (double?)h.DurationMs, ct);

        await Task.WhenAll(
            httpCountTask,
            handlerCountTask,
            entityCountTask,
            errorCountTask,
            activeUsersTask,
            avgResponseTask);

        // Get hourly activity for last 24 hours
        var hourlyActivity = await GetHourlyActivityAsync(tenantId, now, ct);

        var stats = new AuditStatsUpdate(
            now,
            await httpCountTask,
            await handlerCountTask,
            await entityCountTask,
            await errorCountTask,
            await activeUsersTask,
            await avgResponseTask ?? 0,
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

        // Entity type breakdown
        var entityTypeBreakdown = await entityQuery
            .GroupBy(e => e.EntityType)
            .Select(g => new EntityTypeBreakdown(
                g.Key,
                g.Count(e => e.Operation == "Added"),
                g.Count(e => e.Operation == "Modified"),
                g.Count(e => e.Operation == "Deleted"),
                g.Count()))
            .OrderByDescending(e => e.Total)
            .Take(10)
            .ToListAsync(ct);

        // Top users
        var topUsers = await httpQuery
            .Where(h => h.UserId != null)
            .GroupBy(h => new { h.UserId, h.UserEmail })
            .Select(g => new UserActivitySummary(
                g.Key.UserId,
                g.Key.UserEmail,
                g.Count(),
                0)) // TODO: Join with entity changes
            .OrderByDescending(u => u.RequestCount)
            .Take(10)
            .ToListAsync(ct);

        // Top handlers
        var topHandlers = await handlerQuery
            .GroupBy(h => h.HandlerName)
            .Select(g => new HandlerBreakdown(
                g.Key,
                g.Count(),
                g.Count(h => h.IsSuccess),
                g.Count(h => !h.IsSuccess),
                g.Average(h => (double)(h.DurationMs ?? 0))))
            .OrderByDescending(h => h.ExecutionCount)
            .Take(10)
            .ToListAsync(ct);

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
