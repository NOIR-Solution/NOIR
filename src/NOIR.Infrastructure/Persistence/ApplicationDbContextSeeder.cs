using NOIR.Infrastructure.Persistence.Seeders;

namespace NOIR.Infrastructure.Persistence;

/// <summary>
/// Orchestrates database seeding by managing migrations and executing individual seeders.
/// Supports two-tier admin model:
/// - Platform Admin (TenantId = null): Cross-tenant system administration
/// - Tenant Admin (TenantId = specific): Within-tenant administration
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
            var tenantStoreContext = services.GetRequiredService<TenantStoreDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            var configuration = services.GetRequiredService<IConfiguration>();

            // Bind platform settings from configuration
            var platformSettings = new PlatformSettings();
            configuration.GetSection(PlatformSettings.SectionName).Bind(platformSettings);

            // === Apply Database Migrations ===
            await ApplyMigrationsAsync(context, tenantStoreContext, logger);

            // === Build Seeder Context ===
            var seederContext = new SeederContext
            {
                DbContext = context,
                TenantStoreContext = tenantStoreContext,
                UserManager = userManager,
                RoleManager = roleManager,
                Logger = logger,
                Configuration = configuration,
                ServiceProvider = services,
                PlatformSettings = platformSettings
            };

            // === Execute Seeders in Order ===
            await ExecuteSeedersAsync(seederContext, services);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    /// <summary>
    /// Applies pending database migrations for both TenantStore and Application contexts.
    /// </summary>
    private static async Task ApplyMigrationsAsync(
        ApplicationDbContext context,
        TenantStoreDbContext tenantStoreContext,
        ILogger logger)
    {
        if (context.Database.IsRelational())
        {
            // Apply TenantStoreDbContext migrations first (creates Tenants table)
            var tenantPendingMigrations = await tenantStoreContext.Database.GetPendingMigrationsAsync();
            if (tenantPendingMigrations.Any())
            {
                logger.LogInformation("Applying {Count} TenantStore pending migrations: {Migrations}",
                    tenantPendingMigrations.Count(),
                    string.Join(", ", tenantPendingMigrations));
                await tenantStoreContext.Database.MigrateAsync();
                logger.LogInformation("Successfully applied TenantStore migrations");
            }

            // Apply ApplicationDbContext migrations
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
            await tenantStoreContext.Database.EnsureCreatedAsync();
            await context.Database.EnsureCreatedAsync();
        }
    }

    /// <summary>
    /// Executes all seeders in the proper order, handling tenant context setup between phases.
    ///
    /// Seeding phases:
    /// Phase 1: System roles (Order 20)
    /// Phase 2: Platform admin user (Order 30)
    /// Phase 3: Email templates, legal pages, SMTP settings (Order 40-46)
    /// Phase 4: Tenant creation (Order 50) - sets tenant context
    /// Phase 5: Tenant roles + tenant admin (after tenant context)
    /// Phase 6: Permissions (Order 10) + Permission templates (Order 90)
    /// Phase 7: Notification preference fixes (Order 100)
    /// </summary>
    private static async Task ExecuteSeedersAsync(SeederContext seederContext, IServiceProvider services)
    {
        var roleSeeder = new RoleSeeder();
        var userSeeder = new UserSeeder();
        var emailTemplateSeeder = new EmailTemplateSeeder();
        var legalPageSeeder = new LegalPageSeeder();
        var tenantSettingSeeder = new TenantSettingSeeder();
        var tenantSeeder = new TenantSeeder();
        var permissionSeeder = new PermissionSeeder();
        var permissionTemplateSeeder = new PermissionTemplateSeeder();
        var notificationPreferenceSeeder = new NotificationPreferenceSeeder();

        // === PHASE 1: System-Level Roles (TenantId = null) ===
        await roleSeeder.SeedAsync(seederContext);

        // === PHASE 2: Platform Admin User (TenantId = null) ===
        await userSeeder.SeedAsync(seederContext);

        // === PHASE 3: Platform Email Templates (TenantId = null) ===
        await emailTemplateSeeder.SeedAsync(seederContext);

        // === PHASE 3b: Platform Legal Pages (TenantId = null) ===
        await legalPageSeeder.SeedAsync(seederContext);

        // === PHASE 3c: Platform SMTP Settings (Mailhog defaults) ===
        await tenantSettingSeeder.SeedAsync(seederContext);

        // === PHASE 4: Default Tenant (if enabled) ===
        await tenantSeeder.SeedAsync(seederContext);

        if (seederContext.DefaultTenant != null)
        {
            // Set default tenant context for seeding tenant-specific data
            SetTenantContext(seederContext, services);

            // === PHASE 5: Tenant-Level Roles ===
            await roleSeeder.SeedTenantRolesAsync(seederContext);

            // === PHASE 6: Default Tenant Admin (if enabled) ===
            if (seederContext.PlatformSettings.DefaultTenant.Admin.Enabled)
            {
                await userSeeder.SeedTenantAdminAsync(seederContext);
            }

            // Fix notification preferences TenantId
            await notificationPreferenceSeeder.SeedAsync(seederContext);
        }

        // === PHASE 7: Permissions (database-backed Permission entities) ===
        await permissionSeeder.SeedAsync(seederContext);

        // === PHASE 8: Permission Templates ===
        await permissionTemplateSeeder.SeedAsync(seederContext);
    }

    /// <summary>
    /// Sets the Finbuckle multi-tenant context to the default tenant for seeding tenant-specific data.
    /// </summary>
    private static void SetTenantContext(SeederContext seederContext, IServiceProvider services)
    {
        var tenantSetter = services.GetService<IMultiTenantContextSetter>();
        var tenantAccessor = services.GetService<IMultiTenantContextAccessor<Tenant>>();

        if (tenantSetter != null && tenantAccessor?.MultiTenantContext?.TenantInfo == null)
        {
            seederContext.Logger.LogInformation(
                "[Seeder] SETTING tenant context to default tenant: {TenantId} ({TenantName})",
                seederContext.DefaultTenant!.Id,
                seederContext.DefaultTenant.Name);

            // Finbuckle v10 requires constructor argument for MultiTenantContext
            tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(seederContext.DefaultTenant);

            seederContext.Logger.LogInformation(
                "[Seeder] Tenant context NOW SET. All subsequent saves will use TenantId: {TenantId}",
                seederContext.DefaultTenant.Id);
        }
    }
}
