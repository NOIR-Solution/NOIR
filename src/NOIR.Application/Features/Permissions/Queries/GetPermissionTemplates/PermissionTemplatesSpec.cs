namespace NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

/// <summary>
/// Specification to retrieve permission templates.
/// Optionally filter by tenant ID.
/// </summary>
public sealed class PermissionTemplatesSpec : Specification<PermissionTemplate>
{
    public PermissionTemplatesSpec(Guid? tenantId = null)
    {
        // Include system templates (TenantId = null) and optionally tenant-specific templates
        if (tenantId.HasValue)
        {
            Query.Where(t => t.TenantId == null || t.TenantId == tenantId.Value);
        }
        else
        {
            // Only system templates
            Query.Where(t => t.TenantId == null);
        }

        Query.Where(t => !t.IsDeleted)
             .Include("Items.Permission")
             .OrderBy(t => t.SortOrder)
             .ThenBy(t => t.Name)
             .TagWith("PermissionTemplates");
    }
}
