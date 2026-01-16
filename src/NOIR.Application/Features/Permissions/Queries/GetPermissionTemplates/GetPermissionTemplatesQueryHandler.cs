namespace NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

/// <summary>
/// Wolverine handler for getting all permission templates.
/// Returns templates with their permission names.
/// Uses IReadRepository with Specification pattern for Clean Architecture compliance.
/// </summary>
public class GetPermissionTemplatesQueryHandler
{
    private readonly IReadRepository<PermissionTemplate, Guid> _repository;

    public GetPermissionTemplatesQueryHandler(IReadRepository<PermissionTemplate, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<PermissionTemplateDto>>> Handle(
        GetPermissionTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new PermissionTemplatesSpec(query.TenantId);
        var templates = await _repository.ListAsync(spec, cancellationToken);

        // Map to DTOs - Permission names are included via navigation property
        var dtos = templates.Select(t => new PermissionTemplateDto(
            t.Id,
            t.Name,
            t.Description,
            t.TenantId,
            t.IsSystem,
            t.IconName,
            t.Color,
            t.SortOrder,
            t.Items
                .Select(i => i.Permission.Name)
                .ToList()
        )).ToList();

        return Result.Success<IReadOnlyList<PermissionTemplateDto>>(dtos);
    }
}
