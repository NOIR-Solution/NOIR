namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds PermissionTemplate entities with predefined role permission sets.
/// </summary>
public class PermissionTemplateSeeder : ISeeder
{
    /// <summary>
    /// Permission templates depend on Permission entities existing first.
    /// </summary>
    public int Order => 90;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        var existingTemplates = await context.DbContext.Set<PermissionTemplate>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetExistingPermissionTemplates")
            .Include(t => t.Items)
            .ToListAsync(ct);

        var existingByName = existingTemplates.ToDictionary(t => t.Name);

        // Get all permissions from DB for linking
        var allPermissions = await context.DbContext.Set<Permission>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetAllPermissionsForTemplate")
            .ToListAsync(ct);
        var permissionsByName = allPermissions.ToDictionary(p => p.Name);

        var templatesToSeed = GetPermissionTemplateDefinitions();
        var newTemplates = new List<PermissionTemplate>();
        var addedItemsCount = 0;

        foreach (var (templateName, templateDef) in templatesToSeed)
        {
            if (!existingByName.TryGetValue(templateName, out var existing))
            {
                var template = PermissionTemplate.CreatePlatformDefault(
                    templateName,
                    templateDef.Description,
                    isSystem: true,
                    templateDef.IconName,
                    templateDef.Color,
                    templateDef.SortOrder);

                // Add permission items
                foreach (var permissionName in templateDef.Permissions)
                {
                    if (permissionsByName.TryGetValue(permissionName, out var permission))
                    {
                        template.AddPermission(permission.Id);
                    }
                    else
                    {
                        context.Logger.LogWarning("Permission {Permission} not found for template {Template}", permissionName, templateName);
                    }
                }

                newTemplates.Add(template);
                context.Logger.LogInformation("Seeding permission template: {Template} with {Count} permissions", templateName, templateDef.Permissions.Count);
            }
            else if (existing.IsSystem)
            {
                // System templates evolve as new permissions are introduced. Backfill any missing
                // permission items on each seed run so existing rows keep up with the schema.
                // Insert via the child DbSet directly so we never mutate the parent template entity —
                // mutating the loaded aggregate root triggered DbUpdateConcurrencyException because
                // audit interceptors bump the parent's RowVersion mid-save.
                foreach (var permissionName in templateDef.Permissions)
                {
                    if (!permissionsByName.TryGetValue(permissionName, out var permission)) continue;
                    if (existing.Items.Any(i => i.PermissionId == permission.Id)) continue;

                    var item = PermissionTemplateItem.Create(existing.Id, permission.Id);
                    await context.DbContext.Set<PermissionTemplateItem>().AddAsync(item, ct);
                    addedItemsCount++;
                    context.Logger.LogInformation("Backfilled permission {Permission} on template {Template}", permissionName, templateName);
                }
            }
        }

        if (newTemplates.Count > 0)
        {
            await context.DbContext.Set<PermissionTemplate>().AddRangeAsync(newTemplates, ct);
        }
        if (newTemplates.Count > 0 || addedItemsCount > 0)
        {
            await context.DbContext.SaveChangesAsync(ct);
            context.Logger.LogInformation("Permission template seed: {NewTemplates} new, {BackfilledItems} permissions backfilled", newTemplates.Count, addedItemsCount);
        }
    }

    /// <summary>
    /// Defines permission templates with their permission sets.
    /// </summary>
    private static Dictionary<string, (string Description, string? IconName, string? Color, int SortOrder, IReadOnlyList<string> Permissions)> GetPermissionTemplateDefinitions()
    {
        return new Dictionary<string, (string, string?, string?, int, IReadOnlyList<string>)>
        {
            ["Full Admin"] = (
                "Complete system access - all permissions across all modules",
                "shield-check",
                "#dc2626",
                0,
                Permissions.All
            ),
            ["User Manager"] = (
                "Manage users and their role assignments",
                "users",
                "#2563eb",
                10,
                [
                    Permissions.UsersRead,
                    Permissions.UsersCreate,
                    Permissions.UsersUpdate,
                    Permissions.UsersDelete,
                    Permissions.UsersManageRoles,
                    Permissions.RolesRead
                ]
            ),
            ["Role Administrator"] = (
                "Create and manage roles with full permission assignment",
                "key",
                "#7c3aed",
                20,
                [
                    Permissions.RolesRead,
                    Permissions.RolesCreate,
                    Permissions.RolesUpdate,
                    Permissions.RolesDelete,
                    Permissions.RolesManagePermissions
                ]
            ),
            ["Tenant Administrator"] = (
                "Manage tenant settings and configurations",
                "building",
                "#059669",
                30,
                [
                    Permissions.TenantsRead,
                    Permissions.TenantsUpdate,
                    Permissions.UsersRead,
                    Permissions.RolesRead
                ]
            ),
            ["Auditor"] = (
                "Read-only access to audit logs and system monitoring",
                "eye",
                "#0891b2",
                40,
                [
                    Permissions.AuditRead,
                    Permissions.AuditExport,
                    Permissions.AuditEntityHistory,
                    Permissions.AuditPolicyRead,
                    Permissions.UsersRead,
                    Permissions.RolesRead
                ]
            ),
            ["Content Editor"] = (
                "Manage email templates and content",
                "file-text",
                "#f59e0b",
                50,
                [
                    Permissions.EmailTemplatesRead,
                    Permissions.EmailTemplatesUpdate
                ]
            ),
            ["Basic User"] = (
                "Standard read-only access for regular users",
                "user",
                "#6b7280",
                100,
                [
                    Permissions.UsersRead
                ]
            )
        };
    }
}
