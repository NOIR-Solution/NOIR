using NOIR.Application.Common.Interfaces;
using NOIR.Application.Features.Audit.DTOs;

namespace NOIR.Application.Features.Audit.Queries.GetEntityVersions;

public sealed class GetEntityVersionsQueryHandler
{
    private readonly IAuditLogQueryService _auditLogQueryService;

    public GetEntityVersionsQueryHandler(IAuditLogQueryService auditLogQueryService)
    {
        _auditLogQueryService = auditLogQueryService;
    }

    public async Task<Result<IReadOnlyList<EntityVersionDto>>> Handle(
        GetEntityVersionsQuery query,
        CancellationToken cancellationToken)
    {
        var versions = await _auditLogQueryService.GetEntityVersionsAsync(
            query.EntityType,
            query.EntityId,
            cancellationToken);

        return Result.Success(versions);
    }
}
