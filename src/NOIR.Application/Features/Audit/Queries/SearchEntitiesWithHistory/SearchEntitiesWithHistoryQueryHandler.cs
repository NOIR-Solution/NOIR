
namespace NOIR.Application.Features.Audit.Queries.SearchEntitiesWithHistory;

public sealed class SearchEntitiesWithHistoryQueryHandler
{
    private readonly IAuditLogQueryService _auditLogQueryService;

    public SearchEntitiesWithHistoryQueryHandler(IAuditLogQueryService auditLogQueryService)
    {
        _auditLogQueryService = auditLogQueryService;
    }

    public async Task<Result<PagedResult<EntitySearchResultDto>>> Handle(
        SearchEntitiesWithHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogQueryService.SearchEntitiesAsync(
            query.EntityType,
            query.SearchTerm,
            query.Page,
            query.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}
