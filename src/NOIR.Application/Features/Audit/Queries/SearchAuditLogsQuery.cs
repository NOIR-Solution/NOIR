namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Query to search audit logs using full-text search.
/// </summary>
public record SearchAuditLogsQuery(
    string SearchTerm,
    int PageNumber = 1,
    int PageSize = 20,
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    AuditSearchScope Scope = AuditSearchScope.All,
    string? EntityType = null,
    string? UserId = null);

/// <summary>
/// Handler for SearchAuditLogsQuery.
/// </summary>
public class SearchAuditLogsQueryHandler
{
    private readonly IAuditSearchService _searchService;
    private readonly ICurrentUser _currentUser;

    public SearchAuditLogsQueryHandler(
        IAuditSearchService searchService,
        ICurrentUser currentUser)
    {
        _searchService = searchService;
        _currentUser = currentUser;
    }

    public async Task<Result<AuditSearchResult>> Handle(
        SearchAuditLogsQuery query,
        CancellationToken ct)
    {
        var request = new AuditSearchRequest(
            query.SearchTerm,
            query.PageNumber,
            query.PageSize,
            _currentUser.TenantId,
            query.FromDate,
            query.ToDate,
            query.Scope,
            query.EntityType,
            query.UserId);

        var result = await _searchService.SearchAsync(request, ct);
        return Result.Success(result);
    }
}
