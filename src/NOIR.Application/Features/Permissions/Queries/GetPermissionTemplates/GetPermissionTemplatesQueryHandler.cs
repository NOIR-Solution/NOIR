namespace NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

/// <summary>
/// Wolverine handler for getting all permission templates.
/// Returns templates with their permission names.
/// Uses DbContext directly since PermissionTemplate is an Entity not AggregateRoot.
/// </summary>
public class GetPermissionTemplatesQueryHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetPermissionTemplatesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<PermissionTemplateDto>>> Handle(
        GetPermissionTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        var templatesQuery = _dbContext.PermissionTemplates
            .Include(t => t.Items)
            .ThenInclude(i => i.Permission)
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name);

        // Include system templates (TenantId = null) and optionally tenant-specific templates
        if (query.TenantId.HasValue)
        {
            templatesQuery = (IOrderedQueryable<PermissionTemplate>)templatesQuery
                .Where(t => t.TenantId == null || t.TenantId == query.TenantId.Value);
        }
        else
        {
            // Only system templates
            templatesQuery = (IOrderedQueryable<PermissionTemplate>)templatesQuery
                .Where(t => t.TenantId == null);
        }

        var templates = await templatesQuery
            .TagWith("GetPermissionTemplates")
            .AsNoTracking()
            .ToListAsync(cancellationToken);

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
