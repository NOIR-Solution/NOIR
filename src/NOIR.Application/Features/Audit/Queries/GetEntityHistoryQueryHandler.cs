namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Wolverine handler for getting entity change history.
/// </summary>
public class GetEntityHistoryQueryHandler
{
    private readonly IAuditQueryService _auditQueryService;

    public GetEntityHistoryQueryHandler(IAuditQueryService auditQueryService)
    {
        _auditQueryService = auditQueryService;
    }

    public async Task<Result<EntityHistoryDto>> Handle(GetEntityHistoryQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.EntityType) || string.IsNullOrWhiteSpace(query.EntityId))
        {
            return Result.Failure<EntityHistoryDto>(
                Error.Validation("EntityType", "EntityType and EntityId are required.", ErrorCodes.Validation.Required));
        }

        var (items, _) = await _auditQueryService.GetEntityHistoryAsync(
            query.EntityType,
            query.EntityId,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        var historyEntries = items.Select(x => new EntityHistoryEntryDto(
            x.Id,
            x.Operation,
            x.EntityDiff,
            x.Timestamp,
            x.Version,
            x.CorrelationId,
            x.HandlerName,
            x.UserId,
            x.UserEmail)).ToList();

        var history = new EntityHistoryDto(
            query.EntityType,
            query.EntityId,
            historyEntries);

        return Result.Success(history);
    }
}
