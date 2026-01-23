
namespace NOIR.Application.Features.Audit.Queries.GetEntityHistory;

public sealed class GetEntityHistoryQueryHandler
{
    private readonly IAuditLogQueryService _auditLogQueryService;

    public GetEntityHistoryQueryHandler(IAuditLogQueryService auditLogQueryService)
    {
        _auditLogQueryService = auditLogQueryService;
    }

    public async Task<Result<PagedResult<EntityHistoryEntryDto>>> Handle(
        GetEntityHistoryQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _auditLogQueryService.GetEntityHistoryAsync(
            query.EntityType,
            query.EntityId,
            query.FromDate,
            query.ToDate,
            query.UserId,
            query.Page,
            query.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}
