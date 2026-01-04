namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Wolverine handler for getting paginated handler audit logs.
/// </summary>
public class GetHandlerAuditLogsQueryHandler
{
    private readonly IAuditQueryService _auditQueryService;

    public GetHandlerAuditLogsQueryHandler(IAuditQueryService auditQueryService)
    {
        _auditQueryService = auditQueryService;
    }

    public async Task<Result<PaginatedList<HandlerAuditDto>>> Handle(
        GetHandlerAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        var dtoQuery = _auditQueryService.GetHandlerAuditLogsQueryable(
            query.HandlerName,
            query.OperationType,
            query.IsSuccess,
            query.FromDate,
            query.ToDate);

        // Project to DTOs
        var mappedQuery = dtoQuery.Select(h => new HandlerAuditDto(
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
            h.EntityChanges.Select(e => new EntityAuditDto(
                e.Id,
                e.EntityType,
                e.EntityId,
                e.Operation,
                e.EntityDiff,
                e.Timestamp,
                e.Version)).ToList()));

        var result = await PaginatedList<HandlerAuditDto>.CreateAsync(
            mappedQuery,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}
