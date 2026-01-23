
namespace NOIR.Application.Features.Audit.Queries.GetAuditableEntityTypes;

public sealed class GetAuditableEntityTypesQueryHandler
{
    private readonly IAuditLogQueryService _auditLogQueryService;

    public GetAuditableEntityTypesQueryHandler(IAuditLogQueryService auditLogQueryService)
    {
        _auditLogQueryService = auditLogQueryService;
    }

    public async Task<Result<IReadOnlyList<string>>> Handle(
        GetAuditableEntityTypesQuery query,
        CancellationToken cancellationToken)
    {
        var entityTypes = await _auditLogQueryService.GetEntityTypesAsync(cancellationToken);
        return Result.Success(entityTypes);
    }
}
