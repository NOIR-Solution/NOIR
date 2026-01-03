namespace NOIR.Infrastructure.Persistence;

/// <summary>
/// Initializes and seeds the database with required data.
/// </summary>
public static class ApplicationDbContextSeeder
{

    public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            // Set default tenant context for seeding (required for multi-tenant query filters)
            var tenantSetter = services.GetService<IMultiTenantContextSetter>();
            var tenantAccessor = services.GetService<IMultiTenantContextAccessor<TenantInfo>>();
            if (tenantSetter != null && tenantAccessor?.MultiTenantContext?.TenantInfo == null)
            {
                // Create a default tenant for seeding operations
                var defaultTenant = new TenantInfo("default", "default", "Default Tenant");
                tenantSetter.MultiTenantContext = new MultiTenantContext<TenantInfo>(defaultTenant);
            }

            // Ensure database is created and migrations are applied
            // MigrateAsync is idempotent - it checks __EFMigrationsHistory and only applies pending migrations
            // EnsureCreatedAsync should ONLY be used for InMemory tests (no migration history tracking)
            if (context.Database.IsRelational())
            {
                // MigrateAsync handles all cases correctly:
                // 1. Database doesn't exist → Creates DB + applies all migrations + records in __EFMigrationsHistory
                // 2. Database exists, no migrations applied → Applies all pending migrations
                // 3. Database exists, some migrations applied → Applies only pending migrations
                // 4. Database exists, all migrations applied → Does nothing (idempotent)
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying {Count} pending migrations: {Migrations}",
                        pendingMigrations.Count(),
                        string.Join(", ", pendingMigrations));
                    await context.Database.MigrateAsync();
                    logger.LogInformation("Successfully applied all pending migrations");
                }
                else
                {
                    logger.LogInformation("Database is up to date, no pending migrations");
                }
            }
            else
            {
                // Non-relational provider (InMemory for tests) - use EnsureCreatedAsync
                await context.Database.EnsureCreatedAsync();
            }

            // Seed roles with permissions
            await SeedRolesAsync(roleManager, logger);

            // Seed admin user
            await SeedAdminUserAsync(userManager, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    internal static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        foreach (var roleName in Roles.Defaults)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role is null)
            {
                role = new IdentityRole(roleName);
                await roleManager.CreateAsync(role);
                logger.LogInformation("Created role: {Role}", roleName);
            }

            // Seed permissions for this role
            if (Roles.DefaultPermissions.TryGetValue(roleName, out var permissions))
            {
                await SeedRolePermissionsAsync(roleManager, role, permissions, logger);
            }
        }
    }

    internal static async Task SeedRolePermissionsAsync(
        RoleManager<IdentityRole> roleManager,
        IdentityRole role,
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

    internal static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string adminEmail = "admin@noir.local";
        const string adminPassword = "123qwe";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser is null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                logger.LogInformation("Created admin user: {Email}", adminEmail);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Failed to create admin user: {Errors}", errors);
            }
        }
        else
        {
            // Ensure admin password matches expected value (useful after password policy changes)
            var passwordValid = await userManager.CheckPasswordAsync(adminUser, adminPassword);
            if (!passwordValid)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                var result = await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
                if (result.Succeeded)
                {
                    logger.LogInformation("Reset admin user password: {Email}", adminEmail);
                }
            }
        }
    }
}
