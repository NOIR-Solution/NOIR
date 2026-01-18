namespace NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

/// <summary>
/// Wolverine handler for getting all permission templates.
/// Returns templates with their permission names.
/// Uses IPermissionTemplateQueryService to abstract data access
/// since PermissionTemplate is not an AggregateRoot.
/// </summary>
public class GetPermissionTemplatesQueryHandler
{
    private readonly IPermissionTemplateQueryService _queryService;

    public GetPermissionTemplatesQueryHandler(IPermissionTemplateQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<Result<IReadOnlyList<PermissionTemplateDto>>> Handle(
        GetPermissionTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        var dtos = await _queryService.GetAllAsync(query.TenantId, cancellationToken);
        return Result.Success(dtos);
    }
}
