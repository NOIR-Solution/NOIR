namespace NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

/// <summary>
/// Wolverine handler for getting all permission templates.
/// Returns templates with their permission names.
/// Uses IApplicationDbContext directly since PermissionTemplate
/// is not an AggregateRoot (no IRepository available for non-aggregate entities).
/// </summary>
public class GetPermissionTemplatesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetPermissionTemplatesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyList<PermissionTemplateDto>>> Handle(
        GetPermissionTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        // Build query - include system templates (TenantId = null) and optionally tenant-specific
        var templatesQuery = _context.PermissionTemplates
            .AsNoTracking()
            .Include(t => t.Items)
                .ThenInclude(i => i.Permission)
            .Where(t => !t.IsDeleted)
            .TagWith("GetPermissionTemplates");

        // Filter by tenant
        if (query.TenantId.HasValue)
        {
            templatesQuery = templatesQuery
                .Where(t => t.TenantId == null || t.TenantId == query.TenantId.Value);
        }
        else
        {
            // Only system templates
            templatesQuery = templatesQuery.Where(t => t.TenantId == null);
        }

        var templates = await templatesQuery
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
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
