using NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

namespace NOIR.Infrastructure.Services;

/// <summary>
/// Query service implementation for permission templates.
/// Uses EF Core to query PermissionTemplate entities.
/// </summary>
public class PermissionTemplateQueryService : IPermissionTemplateQueryService, IScopedService
{
    private readonly IApplicationDbContext _context;

    public PermissionTemplateQueryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PermissionTemplateDto>> GetAllAsync(
        string? tenantId,
        CancellationToken cancellationToken = default)
    {
        // Build query - include system templates (TenantId = null) and optionally tenant-specific
        var templatesQuery = _context.PermissionTemplates
            .AsNoTracking()
            .Include(t => t.Items)
                .ThenInclude(i => i.Permission)
            .Where(t => !t.IsDeleted)
            .TagWith("GetPermissionTemplates");

        // Filter by tenant
        if (!string.IsNullOrEmpty(tenantId))
        {
            templatesQuery = templatesQuery
                .Where(t => t.TenantId == null || t.TenantId == tenantId);
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
        return templates.Select(t => new PermissionTemplateDto(
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
    }
}
