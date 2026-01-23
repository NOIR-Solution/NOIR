namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds system-level and tenant-level roles with their permission assignments.
/// System roles (TenantId = null) are for platform administration across all tenants.
/// Tenant roles are for within-tenant administration.
/// </summary>
public class RoleSeeder : ISeeder
{
    /// <summary>
    /// Roles must be seeded before users (users are assigned roles).
    /// System roles run first, tenant roles run later (after tenant context is set).
    /// </summary>
    public int Order => 20;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        // === Seed System-Level Roles (TenantId = null) ===
        await SeedSystemRolesAsync(context.RoleManager, context.Logger);
    }

    /// <summary>
    /// Seeds tenant-level roles. Called separately after tenant context is established.
    /// </summary>
    public async Task SeedTenantRolesAsync(SeederContext context, CancellationToken ct = default)
    {
        await SeedTenantRolesInternalAsync(context.RoleManager, context.Logger);
    }

    /// <summary>
    /// Seeds system-level roles (TenantId = null).
    /// These roles are for platform administration across all tenants.
    /// </summary>
    internal static async Task SeedSystemRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var roleDefinitions = new Dictionary<string, (string? Description, int SortOrder, string? IconName, string? Color, bool IsPlatformRole)>
        {
            [Roles.PlatformAdmin] = ("Platform administrator with full cross-tenant access", 0, "crown", "purple", true)
        };

        foreach (var roleName in Roles.SystemRoles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            var definition = roleDefinitions.GetValueOrDefault(roleName);

            if (role is null)
            {
                role = ApplicationRole.Create(
                    roleName,
                    definition.Description,
                    parentRoleId: null,
                    tenantId: null,  // System role - no tenant
                    isSystemRole: true,
                    isPlatformRole: definition.IsPlatformRole,
                    definition.SortOrder,
                    definition.IconName,
                    definition.Color);
                await roleManager.CreateAsync(role);
                logger.LogInformation("Created system role: {Role} (IsPlatformRole={IsPlatformRole})", roleName, definition.IsPlatformRole);
            }
            else
            {
                // Ensure existing role is marked as system role and platform role
                var needsUpdate = false;
                if (!role.IsSystemRole)
                {
                    var isSystemRoleProp = typeof(ApplicationRole).GetProperty(nameof(ApplicationRole.IsSystemRole));
                    isSystemRoleProp?.SetValue(role, true);
                    needsUpdate = true;
                }
                if (definition.IsPlatformRole && !role.IsPlatformRole)
                {
                    var isPlatformRoleProp = typeof(ApplicationRole).GetProperty(nameof(ApplicationRole.IsPlatformRole));
                    isPlatformRoleProp?.SetValue(role, true);
                    needsUpdate = true;
                }
                if (needsUpdate)
                {
                    role.Update(
                        roleName,
                        definition.Description,
                        parentRoleId: null,
                        definition.SortOrder,
                        definition.IconName,
                        definition.Color);
                    await roleManager.UpdateAsync(role);
                    logger.LogInformation("Updated role {Role} to system/platform role", roleName);
                }
            }

            // Seed permissions for this role
            if (Roles.DefaultPermissions.TryGetValue(roleName, out var permissions))
            {
                await SeedRolePermissionsAsync(roleManager, role, permissions, logger);
            }
        }
    }

    /// <summary>
    /// Seeds tenant-level roles (can be assigned to users within a tenant).
    /// These roles are for within-tenant administration.
    /// </summary>
    internal static async Task SeedTenantRolesInternalAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        var roleDefinitions = new Dictionary<string, (string? Description, int SortOrder, string? IconName, string? Color)>
        {
            [Roles.Admin] = ("Full system access with all permissions within tenant", 10, "shield", "red"),
            [Roles.User] = ("Standard user access within tenant", 20, "user", "blue")
        };

        foreach (var roleName in Roles.TenantRoles)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            var definition = roleDefinitions.GetValueOrDefault(roleName);

            if (role is null)
            {
                role = ApplicationRole.Create(
                    roleName,
                    definition.Description,
                    parentRoleId: null,
                    tenantId: null,  // Tenant roles are still defined at system level but used within tenants
                    isSystemRole: true,  // System-defined (not user-created)
                    isPlatformRole: false,  // Tenant roles are visible in tenant UI
                    definition.SortOrder,
                    definition.IconName,
                    definition.Color);
                await roleManager.CreateAsync(role);
                logger.LogInformation("Created tenant role: {Role}", roleName);
            }
            else
            {
                // Update existing role to be a system role with proper properties
                if (!role.IsSystemRole)
                {
                    role.Update(
                        roleName,
                        definition.Description,
                        parentRoleId: null,
                        definition.SortOrder,
                        definition.IconName,
                        definition.Color);
                    var isSystemRoleProp = typeof(ApplicationRole).GetProperty(nameof(ApplicationRole.IsSystemRole));
                    isSystemRoleProp?.SetValue(role, true);
                    await roleManager.UpdateAsync(role);
                    logger.LogInformation("Updated role {Role} to system role", roleName);
                }
            }

            // Seed permissions for this role
            if (Roles.DefaultPermissions.TryGetValue(roleName, out var permissions))
            {
                await SeedRolePermissionsAsync(roleManager, role, permissions, logger);
            }
        }
    }

    internal static async Task SeedRolePermissionsAsync(
        RoleManager<ApplicationRole> roleManager,
        ApplicationRole role,
        IReadOnlyList<string> permissions,
        ILogger logger)
    {
        var existingClaims = await roleManager.GetClaimsAsync(role);
        var existingPermissions = existingClaims
            .Where(c => c.Type == Permissions.ClaimType)
            .Select(c => c.Value)
            .ToHashSet();

        foreach (var permission in permissions)
        {
            if (!existingPermissions.Contains(permission))
            {
                await roleManager.AddClaimAsync(role, new Claim(Permissions.ClaimType, permission));
                logger.LogInformation("Added permission {Permission} to role {Role}", permission, role.Name);
            }
        }
    }
}
