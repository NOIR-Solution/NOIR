namespace NOIR.Infrastructure.Audit;

/// <summary>
/// Implementation of IAuditSearchService using SQL Server Full-Text Search.
/// Provides unified search across all audit log types.
/// </summary>
public class AuditSearchService : IAuditSearchService, IScopedService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuditSearchService> _logger;

    public AuditSearchService(
        ApplicationDbContext dbContext,
        ILogger<AuditSearchService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AuditSearchResult> SearchAsync(
        AuditSearchRequest request,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SearchTerm);

        _logger.LogDebug(
            "Searching audit logs. Term: {SearchTerm}, Scope: {Scope}",
            request.SearchTerm, request.Scope);

        var searchTerm = SanitizeSearchTerm(request.SearchTerm);
        var allHits = new List<AuditSearchHit>();

        // Search HTTP Request Audit Logs
        if (request.Scope.HasFlag(AuditSearchScope.HttpRequests))
        {
            var httpHits = await SearchHttpRequestsAsync(searchTerm, request, ct);
            allHits.AddRange(httpHits);
        }

        // Search Handler Audit Logs
        if (request.Scope.HasFlag(AuditSearchScope.Handlers))
        {
            var handlerHits = await SearchHandlersAsync(searchTerm, request, ct);
            allHits.AddRange(handlerHits);
        }

        // Search Entity Audit Logs
        if (request.Scope.HasFlag(AuditSearchScope.Entities))
        {
            var entityHits = await SearchEntitiesAsync(searchTerm, request, ct);
            allHits.AddRange(entityHits);
        }

        // Sort by rank (descending) then timestamp (descending)
        var sortedHits = allHits
            .OrderByDescending(h => h.Rank)
            .ThenByDescending(h => h.Timestamp)
            .ToList();

        var totalCount = sortedHits.Count;

        // Apply pagination
        var pagedHits = sortedHits
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new AuditSearchResult(
            request.SearchTerm,
            totalCount,
            request.PageNumber,
            request.PageSize,
            pagedHits);
    }

    private async Task<IReadOnlyList<AuditSearchHit>> SearchHttpRequestsAsync(
        string searchTerm,
        AuditSearchRequest request,
        CancellationToken ct)
    {
        // Use CONTAINS for full-text search
        // Note: FTS must be configured on the table for this to work
        var sql = @"
            SELECT h.Id, h.CorrelationId, h.Url, h.HttpMethod, h.StartTime, h.UserId, h.UserEmail,
                   KEY_TBL.RANK as SearchRank
            FROM HttpRequestAuditLogs h
            INNER JOIN CONTAINSTABLE(HttpRequestAuditLogs, *, {0}) AS KEY_TBL
                ON h.Id = KEY_TBL.[KEY]
            WHERE (@TenantId IS NULL OR h.TenantId = @TenantId)
              AND (@FromDate IS NULL OR h.StartTime >= @FromDate)
              AND (@ToDate IS NULL OR h.StartTime <= @ToDate)
              AND (@UserId IS NULL OR h.UserId = @UserId)
            ORDER BY KEY_TBL.RANK DESC, h.StartTime DESC";

        try
        {
            var results = await _dbContext.Database
                .SqlQueryRaw<HttpSearchResult>(
                    sql,
                    new Microsoft.Data.SqlClient.SqlParameter("@SearchTerm", $"\"{searchTerm}*\""),
                    new Microsoft.Data.SqlClient.SqlParameter("@TenantId", request.TenantId ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@FromDate", request.FromDate ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@ToDate", request.ToDate ?? (object)DBNull.Value),
                    new Microsoft.Data.SqlClient.SqlParameter("@UserId", request.UserId ?? (object)DBNull.Value))
                .TagWith("AuditSearchService.SearchHttpRequests")
                .Take(100) // Limit per type
                .ToListAsync(ct);

            return results.Select(r => new AuditSearchHit(
                r.Id,
                AuditSearchHitType.HttpRequest,
                r.CorrelationId,
                $"{r.HttpMethod} {r.Url}",
                null,
                r.StartTime,
                r.UserId,
                r.UserEmail,
                r.SearchRank)).ToList();
        }
        catch (Exception ex)
        {
            // Fall back to LIKE search if FTS is not available
            _logger.LogWarning(ex, "Full-text search failed for HTTP requests, falling back to LIKE");
            return await SearchHttpRequestsWithLikeAsync(searchTerm, request, ct);
        }
    }

    private async Task<IReadOnlyList<AuditSearchHit>> SearchHttpRequestsWithLikeAsync(
        string searchTerm,
        AuditSearchRequest request,
        CancellationToken ct)
    {
        var query = _dbContext.HttpRequestAuditLogs
            .AsNoTracking()
            .TagWith("AuditSearchService.SearchHttpRequests_LIKE")
            .Where(h =>
                h.Url.Contains(searchTerm) ||
                (h.RequestBody != null && h.RequestBody.Contains(searchTerm)) ||
                (h.ResponseBody != null && h.ResponseBody.Contains(searchTerm)) ||
                (h.UserEmail != null && h.UserEmail.Contains(searchTerm)));

        if (!string.IsNullOrEmpty(request.TenantId))
            query = query.Where(h => h.TenantId == request.TenantId);

        if (request.FromDate.HasValue)
            query = query.Where(h => h.StartTime >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(h => h.StartTime <= request.ToDate.Value);

        if (!string.IsNullOrEmpty(request.UserId))
            query = query.Where(h => h.UserId == request.UserId);

        var results = await query
            .OrderByDescending(h => h.StartTime)
            .Take(100)
            .Select(h => new AuditSearchHit(
                h.Id,
                AuditSearchHitType.HttpRequest,
                h.CorrelationId,
                $"{h.HttpMethod} {h.Url}",
                null,
                h.StartTime,
                h.UserId,
                h.UserEmail,
                50)) // Default rank for LIKE matches
            .ToListAsync(ct);

        return results;
    }

    private async Task<IReadOnlyList<AuditSearchHit>> SearchHandlersAsync(
        string searchTerm,
        AuditSearchRequest request,
        CancellationToken ct)
    {
        // Fall back to LIKE search (FTS implementation similar to above)
        var query = _dbContext.HandlerAuditLogs
            .AsNoTracking()
            .TagWith("AuditSearchService.SearchHandlers")
            .Where(h =>
                h.HandlerName.Contains(searchTerm) ||
                (h.InputParameters != null && h.InputParameters.Contains(searchTerm)) ||
                (h.OutputResult != null && h.OutputResult.Contains(searchTerm)) ||
                (h.ErrorMessage != null && h.ErrorMessage.Contains(searchTerm)));

        if (!string.IsNullOrEmpty(request.TenantId))
            query = query.Where(h => h.TenantId == request.TenantId);

        if (request.FromDate.HasValue)
            query = query.Where(h => h.StartTime >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(h => h.StartTime <= request.ToDate.Value);

        var results = await query
            .OrderByDescending(h => h.StartTime)
            .Take(100)
            .Select(h => new AuditSearchHit(
                h.Id,
                AuditSearchHitType.Handler,
                h.CorrelationId,
                h.HandlerName,
                h.ErrorMessage,
                h.StartTime,
                null,
                null,
                50))
            .ToListAsync(ct);

        return results;
    }

    private async Task<IReadOnlyList<AuditSearchHit>> SearchEntitiesAsync(
        string searchTerm,
        AuditSearchRequest request,
        CancellationToken ct)
    {
        var query = _dbContext.EntityAuditLogs
            .AsNoTracking()
            .TagWith("AuditSearchService.SearchEntities")
            .Where(e =>
                e.EntityType.Contains(searchTerm) ||
                e.EntityId.Contains(searchTerm) ||
                (e.EntityDiff != null && e.EntityDiff.Contains(searchTerm)));

        if (!string.IsNullOrEmpty(request.TenantId))
            query = query.Where(e => e.TenantId == request.TenantId);

        if (request.FromDate.HasValue)
            query = query.Where(e => e.Timestamp >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(e => e.Timestamp <= request.ToDate.Value);

        if (!string.IsNullOrEmpty(request.EntityType))
            query = query.Where(e => e.EntityType == request.EntityType);

        var results = await query
            .OrderByDescending(e => e.Timestamp)
            .Take(100)
            .Select(e => new AuditSearchHit(
                e.Id,
                AuditSearchHitType.Entity,
                e.CorrelationId,
                $"{e.Operation}: {e.EntityType} ({e.EntityId})",
                TruncateSnippet(e.EntityDiff, 200),
                e.Timestamp,
                null,
                null,
                50))
            .ToListAsync(ct);

        return results;
    }

    private static string SanitizeSearchTerm(string term)
    {
        // Remove characters that could break FTS queries
        var sanitized = term
            .Replace("\"", "")
            .Replace("'", "")
            .Replace("*", "")
            .Replace("?", "")
            .Trim();

        return sanitized;
    }

    private static string? TruncateSnippet(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return null;
        if (text.Length <= maxLength) return text;
        return text[..maxLength] + "...";
    }

    // Internal class for raw SQL mapping
    private class HttpSearchResult
    {
        public Guid Id { get; set; }
        public string CorrelationId { get; set; } = default!;
        public string Url { get; set; } = default!;
        public string HttpMethod { get; set; } = default!;
        public DateTimeOffset StartTime { get; set; }
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public int SearchRank { get; set; }
    }
}
