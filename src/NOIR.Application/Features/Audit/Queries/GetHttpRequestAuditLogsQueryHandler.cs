namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Wolverine handler for getting paginated HTTP request audit logs.
/// </summary>
public class GetHttpRequestAuditLogsQueryHandler
{
    private readonly IAuditQueryService _auditQueryService;

    public GetHttpRequestAuditLogsQueryHandler(IAuditQueryService auditQueryService)
    {
        _auditQueryService = auditQueryService;
    }

    public async Task<Result<PaginatedList<HttpRequestAuditDto>>> Handle(
        GetHttpRequestAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        var dtoQuery = _auditQueryService.GetHttpRequestAuditLogsQueryable(
            query.UserId,
            query.HttpMethod,
            query.StatusCode,
            query.FromDate,
            query.ToDate);

        // Project to DTOs
        var mappedQuery = dtoQuery.Select(h => new HttpRequestAuditDto(
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
            h.HandlerCount,
            h.EntityChangeCount));

        var result = await PaginatedList<HttpRequestAuditDto>.CreateAsync(
            mappedQuery,
            query.PageNumber,
            query.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}
