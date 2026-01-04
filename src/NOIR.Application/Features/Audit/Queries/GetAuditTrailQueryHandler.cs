namespace NOIR.Application.Features.Audit.Queries;

/// <summary>
/// Wolverine handler for getting the complete audit trail by correlation ID.
/// </summary>
public class GetAuditTrailQueryHandler
{
    private readonly IAuditQueryService _auditQueryService;

    public GetAuditTrailQueryHandler(IAuditQueryService auditQueryService)
    {
        _auditQueryService = auditQueryService;
    }

    public async Task<Result<AuditTrailDto>> Handle(GetAuditTrailQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.CorrelationId))
        {
            return Result.Failure<AuditTrailDto>(
                Error.Validation("CorrelationId", "CorrelationId is required.", ErrorCodes.Validation.Required));
        }

        var trail = await _auditQueryService.GetAuditTrailAsync(query.CorrelationId, cancellationToken);

        if (trail is null)
        {
            return Result.Success(new AuditTrailDto(
                query.CorrelationId,
                null,
                [],
                []));
        }

        // Map to DTOs
        var entityDtos = trail.EntityChanges.Select(e => new EntityAuditDto(
            e.Id,
            e.EntityType,
            e.EntityId,
            e.Operation,
            e.EntityDiff,
            e.Timestamp,
            e.Version)).ToList();

        var handlerDtos = trail.Handlers.Select(h => new HandlerAuditDto(
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
                e.Version)).ToList()
        )).ToList();

        HttpRequestAuditDetailDto? httpDto = null;
        if (trail.HttpRequest is not null)
        {
            httpDto = new HttpRequestAuditDetailDto(
                trail.HttpRequest.Id,
                trail.HttpRequest.CorrelationId,
                trail.HttpRequest.HttpMethod,
                trail.HttpRequest.Url,
                trail.HttpRequest.QueryString,
                trail.HttpRequest.RequestHeaders,
                trail.HttpRequest.RequestBody,
                trail.HttpRequest.ResponseStatusCode,
                trail.HttpRequest.ResponseBody,
                trail.HttpRequest.UserId,
                trail.HttpRequest.UserEmail,
                trail.HttpRequest.TenantId,
                trail.HttpRequest.IpAddress,
                trail.HttpRequest.UserAgent,
                trail.HttpRequest.StartTime,
                trail.HttpRequest.EndTime,
                trail.HttpRequest.DurationMs,
                handlerDtos);
        }

        var result = new AuditTrailDto(
            query.CorrelationId,
            httpDto,
            handlerDtos,
            entityDtos);

        return Result.Success(result);
    }
}
