namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds platform admin and tenant admin users.
/// Supports two-tier admin model:
/// - Platform Admin (TenantId = null): Cross-tenant system administration
/// - Tenant Admin (TenantId = specific): Within-tenant administration
/// </summary>
public class UserSeeder : ISeeder
{
    /// <summary>
    /// Users must be seeded after roles (users are assigned to roles).
    /// </summary>
    public int Order => 30;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        // Seed Platform Admin User (TenantId = null)
        await SeedPlatformAdminUserAsync(
            context.UserManager,
            context.PlatformSettings.PlatformAdmin,
            context.Logger);
    }

    /// <summary>
    /// Seeds tenant admin user. Called separately after tenant context is established.
    /// </summary>
    public async Task SeedTenantAdminAsync(SeederContext context, CancellationToken ct = default)
    {
        if (context.DefaultTenant == null) return;

        await SeedTenantAdminUserAsync(
            context.UserManager,
            context.DefaultTenant.Id,
            context.PlatformSettings.DefaultTenant.Admin,
            context.Logger);
    }

    /// <summary>
    /// Seeds the platform admin user (TenantId = null).
    /// This user has access to all tenants and platform-level operations.
    /// </summary>
    internal static async Task SeedPlatformAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        PlatformAdminSettings settings,
        ILogger logger)
    {
        var adminUser = await userManager.FindByEmailAsync(settings.Email);

        if (adminUser is null)
        {
            logger.LogInformation("[Seeder] Creating NEW platform admin user: {Email}", settings.Email);

            adminUser = new ApplicationUser
            {
                UserName = settings.Email,
                Email = settings.Email,
                FirstName = settings.FirstName,
                LastName = settings.LastName,
                EmailConfirmed = true,
                IsActive = true,
                IsSystemUser = true,
                TenantId = null,  // Platform admin has NO tenant - cross-tenant access
                CreatedAt = DateTimeOffset.UtcNow
            };

            logger.LogInformation(
                "[Seeder] BEFORE CreateAsync: Email={Email}, IsSystemUser={IsSystemUser}, TenantId={TenantId}",
                adminUser.Email,
                adminUser.IsSystemUser,
                adminUser.TenantId ?? "NULL");

            var result = await userManager.CreateAsync(adminUser, settings.Password);

            if (result.Succeeded)
            {
                logger.LogInformation(
                    "[Seeder] AFTER CreateAsync: Email={Email}, IsSystemUser={IsSystemUser}, TenantId={TenantId}",
                    adminUser.Email,
                    adminUser.IsSystemUser,
                    adminUser.TenantId ?? "NULL");

                logger.LogInformation("[Seeder] Adding PlatformAdmin role to user: {Email}", settings.Email);
                await userManager.AddToRoleAsync(adminUser, Roles.PlatformAdmin);

                logger.LogInformation(
                    "[Seeder] AFTER AddToRoleAsync: Email={Email}, IsSystemUser={IsSystemUser}, TenantId={TenantId}",
                    adminUser.Email,
                    adminUser.IsSystemUser,
                    adminUser.TenantId ?? "NULL");

                logger.LogInformation("Created platform admin user: {Email} (TenantId = {TenantId})", settings.Email, adminUser.TenantId ?? "NULL");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create platform admin user: {Errors}", errors);
            }
        }
        else
        {
            var needsUpdate = false;

            // Ensure platform admin is marked as system user
            if (!adminUser.IsSystemUser)
            {
                adminUser.IsSystemUser = true;
                needsUpdate = true;
                logger.LogInformation("Marked platform admin user as system user: {Email}", settings.Email);
            }

            // Ensure platform admin has NO TenantId (cross-tenant access)
            if (!string.IsNullOrEmpty(adminUser.TenantId))
            {
                adminUser.TenantId = null;
                needsUpdate = true;
                logger.LogInformation("Cleared TenantId for platform admin user: {Email} (now cross-tenant)", settings.Email);
            }

            if (needsUpdate)
            {
                await userManager.UpdateAsync(adminUser);
            }

            // Ensure platform admin has PlatformAdmin role
            if (!await userManager.IsInRoleAsync(adminUser, Roles.PlatformAdmin))
            {
                await userManager.AddToRoleAsync(adminUser, Roles.PlatformAdmin);
                logger.LogInformation("Added PlatformAdmin role to user: {Email}", settings.Email);
            }

            // Ensure platform admin password matches expected value
            var passwordValid = await userManager.CheckPasswordAsync(adminUser, settings.Password);
            if (!passwordValid)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                var result = await userManager.ResetPasswordAsync(adminUser, token, settings.Password);
                if (result.Succeeded)
                {
                    logger.LogInformation("Reset platform admin user password: {Email}", settings.Email);
                }
            }
        }
    }

    /// <summary>
    /// Seeds the tenant admin user for a specific tenant.
    /// This user has Admin role within the specified tenant only.
    /// </summary>
    internal static async Task SeedTenantAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        string tenantId,
        TenantAdminSettings settings,
        ILogger logger)
    {
        var adminUser = await userManager.FindByEmailAsync(settings.Email);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = settings.Email,
                Email = settings.Email,
                FirstName = settings.FirstName,
                LastName = settings.LastName,
                EmailConfirmed = true,
                IsActive = true,
                IsSystemUser = true,
                TenantId = tenantId,  // Scoped to specific tenant
                CreatedAt = DateTimeOffset.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, settings.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                logger.LogInformation("Created tenant admin user: {Email} in tenant {TenantId}", settings.Email, tenantId);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create tenant admin user: {Errors}", errors);
            }
        }
        else
        {
            var needsUpdate = false;

            // Ensure admin is marked as system user (for upgrades from older versions)
            if (!adminUser.IsSystemUser)
            {
                adminUser.IsSystemUser = true;
                needsUpdate = true;
                logger.LogInformation("Marked tenant admin user as system user: {Email}", settings.Email);
            }

            // Ensure admin has TenantId assigned (for upgrades to single-tenant-per-user model)
            if (string.IsNullOrEmpty(adminUser.TenantId))
            {
                adminUser.TenantId = tenantId;
                needsUpdate = true;
                logger.LogInformation("Assigned tenant {TenantId} to admin user: {Email}", tenantId, settings.Email);
            }

            if (needsUpdate)
            {
                await userManager.UpdateAsync(adminUser);
            }

            // Ensure admin has Admin role
            if (!await userManager.IsInRoleAsync(adminUser, Roles.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                logger.LogInformation("Added Admin role to user: {Email}", settings.Email);
            }

            // Ensure admin password matches expected value (useful after password policy changes)
            var passwordValid = await userManager.CheckPasswordAsync(adminUser, settings.Password);
            if (!passwordValid)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                var result = await userManager.ResetPasswordAsync(adminUser, token, settings.Password);
                if (result.Succeeded)
                {
                    logger.LogInformation("Reset tenant admin user password: {Email}", settings.Email);
                }
            }
        }
    }
}
