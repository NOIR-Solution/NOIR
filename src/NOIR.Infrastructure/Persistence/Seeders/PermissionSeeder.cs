namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds Permission entities based on the Permissions constants.
/// These enable database-backed permission management alongside claims.
/// </summary>
public class PermissionSeeder : ISeeder
{
    /// <summary>
    /// Permissions must be seeded before roles (roles reference permissions for templates).
    /// </summary>
    public int Order => 10;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        var existingPermissions = await context.DbContext.Set<Permission>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetExistingPermissions")
            .ToListAsync(ct);

        var existingByName = existingPermissions.ToDictionary(p => p.Name);
        var permissionsToSeed = GetPermissionDefinitions();
        var newPermissions = new List<Permission>();

        foreach (var permission in permissionsToSeed)
        {
            if (!existingByName.ContainsKey(permission.Name))
            {
                newPermissions.Add(permission);
                context.Logger.LogInformation("Seeding permission: {Permission}", permission.Name);
            }
        }

        if (newPermissions.Count > 0)
        {
            await context.DbContext.Set<Permission>().AddRangeAsync(newPermissions, ct);
            await context.DbContext.SaveChangesAsync(ct);
            context.Logger.LogInformation("Seeded {Count} permissions", newPermissions.Count);
        }
    }

    /// <summary>
    /// Builds a list of Permission entities from the Permissions constants.
    /// </summary>
    private static List<Permission> GetPermissionDefinitions()
    {
        var permissions = new List<Permission>();
        var sortOrder = 0;

        // Users category
        permissions.Add(Permission.Create("users", "read", "View Users", null, "View user profiles and list users", "User Management", true, sortOrder++));
        permissions.Add(Permission.Create("users", "create", "Create Users", null, "Create new user accounts", "User Management", true, sortOrder++));
        permissions.Add(Permission.Create("users", "update", "Update Users", null, "Edit user profiles and settings", "User Management", true, sortOrder++));
        permissions.Add(Permission.Create("users", "delete", "Delete Users", null, "Delete user accounts", "User Management", true, sortOrder++));
        permissions.Add(Permission.Create("users", "manage-roles", "Manage User Roles", null, "Assign and remove roles from users", "User Management", true, sortOrder++));

        // Roles category
        permissions.Add(Permission.Create("roles", "read", "View Roles", null, "View roles and their permissions", "Role Management", true, sortOrder++));
        permissions.Add(Permission.Create("roles", "create", "Create Roles", null, "Create new roles", "Role Management", true, sortOrder++));
        permissions.Add(Permission.Create("roles", "update", "Update Roles", null, "Edit role details", "Role Management", true, sortOrder++));
        permissions.Add(Permission.Create("roles", "delete", "Delete Roles", null, "Delete roles", "Role Management", true, sortOrder++));
        permissions.Add(Permission.Create("roles", "manage-permissions", "Manage Role Permissions", null, "Assign and remove permissions from roles", "Role Management", true, sortOrder++));

        // Tenants category
        permissions.Add(Permission.Create("tenants", "read", "View Tenants", null, "View tenant information", "Tenant Management", true, sortOrder++));
        permissions.Add(Permission.Create("tenants", "create", "Create Tenants", null, "Create new tenants", "Tenant Management", true, sortOrder++));
        permissions.Add(Permission.Create("tenants", "update", "Update Tenants", null, "Edit tenant settings", "Tenant Management", true, sortOrder++));
        permissions.Add(Permission.Create("tenants", "delete", "Delete Tenants", null, "Delete tenants", "Tenant Management", true, sortOrder++));

        // System category
        permissions.Add(Permission.Create("system", "admin", "System Admin", null, "Full system administration access", "System", true, sortOrder++));
        permissions.Add(Permission.Create("system", "audit-logs", "View Audit Logs", null, "Access system audit logs", "System", true, sortOrder++));
        permissions.Add(Permission.Create("system", "settings", "Manage System Settings", null, "Configure system settings", "System", true, sortOrder++));
        permissions.Add(Permission.Create("system", "hangfire", "Hangfire Dashboard", null, "Access Hangfire background job dashboard", "System", true, sortOrder++));

        // Configuration Management category
        permissions.Add(Permission.Create("system", "config:view", "View Configuration", null, "View platform configuration settings", "Configuration Management", true, sortOrder++));
        permissions.Add(Permission.Create("system", "config:edit", "Edit Configuration", null, "Edit platform configuration settings", "Configuration Management", true, sortOrder++));
        permissions.Add(Permission.Create("system", "app:restart", "Restart Application", null, "Restart the application", "Configuration Management", true, sortOrder++));

        // Audit category
        permissions.Add(Permission.Create("audit", "read", "View Audit Data", null, "View audit records", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "export", "Export Audit Data", null, "Export audit logs to files", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "entity-history", "View Entity History", null, "View change history for entities", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "policy-read", "Read Audit Policies", null, "View audit policy configurations", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "policy-write", "Write Audit Policies", null, "Create and edit audit policies", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "policy-delete", "Delete Audit Policies", null, "Delete audit policies", "Audit", true, sortOrder++));
        permissions.Add(Permission.Create("audit", "stream", "Stream Audit Events", null, "Access real-time audit event stream", "Audit", true, sortOrder++));

        // Email Templates category
        permissions.Add(Permission.Create("email-templates", "read", "View Email Templates", null, "View email templates", "Email Templates", true, sortOrder++));
        permissions.Add(Permission.Create("email-templates", "update", "Update Email Templates", null, "Edit email template content", "Email Templates", true, sortOrder++));

        return permissions;
    }
}
