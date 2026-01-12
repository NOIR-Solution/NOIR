namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for full-text search across all audit logs.
/// </summary>
public interface IAuditSearchService
{
    /// <summary>
    /// Performs a unified full-text search across all audit log types.
    /// </summary>
    Task<AuditSearchResult> SearchAsync(
        AuditSearchRequest request,
        CancellationToken ct = default);
}

/// <summary>
/// Request for audit log full-text search.
/// </summary>
public record AuditSearchRequest(
    string SearchTerm,
    int PageNumber = 1,
    int PageSize = 20,
    string? TenantId = null,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    AuditSearchScope Scope = AuditSearchScope.All,
    string? EntityType = null,
    string? UserId = null);

/// <summary>
/// Scope of the audit search.
/// </summary>
[Flags]
public enum AuditSearchScope
{
    None = 0,
    HttpRequests = 1,
    Handlers = 2,
    Entities = 4,
    All = HttpRequests | Handlers | Entities
}

/// <summary>
/// Result of audit log full-text search.
/// </summary>
public record AuditSearchResult(
    string SearchTerm,
    int TotalCount,
    int PageNumber,
    int PageSize,
    IReadOnlyList<AuditSearchHit> Hits);

/// <summary>
/// Single search hit with relevance ranking.
/// </summary>
public record AuditSearchHit(
    Guid Id,
    AuditSearchHitType Type,
    string CorrelationId,
    string Title,
    string? Snippet,
    DateTimeOffset Timestamp,
    string? UserId,
    string? UserEmail,
    int Rank);

/// <summary>
/// Type of audit search hit.
/// </summary>
public enum AuditSearchHitType
{
    HttpRequest,
    Handler,
    Entity
}
