namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Orchestrates demo seed data creation across all tenants.
/// Called from ApplicationDbContextSeeder after all system seeders complete.
/// Only runs in Development when SeedData:Enabled is true.
/// </summary>
public static class SeedDataOrchestrator
{
    public static async Task ExecuteAsync(
        SeederContext seederContext,
        IServiceProvider services,
        CancellationToken ct = default)
    {
        // 1. Environment guard — never seed production
        var env = services.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment())
        {
            return;
        }

        // 2. Configuration check
        var settings = new SeedDataSettings();
        seederContext.Configuration.GetSection(SeedDataSettings.SectionName).Bind(settings);
        if (!settings.Enabled)
        {
            seederContext.Logger.LogInformation("[SeedData] Disabled via configuration. Skipping.");
            return;
        }

        var logger = seederContext.Logger;
        logger.LogInformation("[SeedData] Starting demo data seeding...");
        var totalSw = Stopwatch.StartNew();

        // 3. Build tenant list (default + additional tenants)
        var tenants = await BuildTenantListAsync(seederContext, settings, services, ct);

        // 4. Collect enabled modules
        var modules = GetEnabledModules(settings);
        if (modules.Count == 0)
        {
            logger.LogInformation("[SeedData] No modules enabled. Skipping.");
            return;
        }

        logger.LogInformation("[SeedData] Enabled modules: {Modules}",
            string.Join(", ", modules.Select(m => m.ModuleName)));

        // 5. Seed per tenant
        foreach (var (tenant, adminUserId) in tenants)
        {
            logger.LogInformation("[SeedData] === Seeding tenant: {Identifier} ({Name}) ===",
                tenant.Identifier, tenant.Name);

            SetTenantContext(tenant, services, logger);

            var context = new SeedDataContext
            {
                DbContext = seederContext.DbContext,
                Logger = logger,
                ServiceProvider = services,
                Settings = settings,
                CurrentTenant = tenant,
                TenantAdminUserId = adminUserId
            };

            foreach (var module in modules)
            {
                var sw = Stopwatch.StartNew();
                logger.LogInformation("[SeedData] {Module} for {Tenant}...",
                    module.ModuleName, tenant.Identifier);

                try
                {
                    await module.SeedAsync(context, ct);
                    logger.LogInformation("[SeedData] {Module} for {Tenant} done ({Ms}ms)",
                        module.ModuleName, tenant.Identifier, sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[SeedData] {Module} for {Tenant} FAILED after {Ms}ms",
                        module.ModuleName, tenant.Identifier, sw.ElapsedMilliseconds);
                    throw;
                }
            }
        }

        totalSw.Stop();
        logger.LogInformation("[SeedData] Completed in {Ms}ms", totalSw.ElapsedMilliseconds);
    }

    /// <summary>
    /// Builds the list of (Tenant, adminUserId) tuples to seed.
    /// Starts with the default tenant, then creates additional tenants from settings.
    /// </summary>
    private static async Task<List<(Tenant Tenant, string AdminUserId)>> BuildTenantListAsync(
        SeederContext seederContext,
        SeedDataSettings settings,
        IServiceProvider services,
        CancellationToken ct)
    {
        var tenants = new List<(Tenant Tenant, string AdminUserId)>();
        var logger = seederContext.Logger;

        // Default tenant (already created by TenantSeeder)
        if (seederContext.DefaultTenant != null)
        {
            var defaultAdminEmail = seederContext.PlatformSettings.DefaultTenant.Admin.Email;
            var defaultAdmin = await seederContext.UserManager.FindByEmailAsync(defaultAdminEmail);
            if (defaultAdmin != null)
            {
                tenants.Add((seederContext.DefaultTenant, defaultAdmin.Id));
                logger.LogInformation("[SeedData] Default tenant: {Identifier} (admin: {Email})",
                    seederContext.DefaultTenant.Identifier, defaultAdminEmail);
            }
            else
            {
                logger.LogWarning("[SeedData] Default tenant admin not found: {Email}. Skipping default tenant.",
                    defaultAdminEmail);
            }
        }

        // Additional tenants from configuration
        foreach (var tenantSettings in settings.AdditionalTenants)
        {
            if (string.IsNullOrWhiteSpace(tenantSettings.Identifier))
            {
                logger.LogWarning("[SeedData] Skipping additional tenant with empty identifier.");
                continue;
            }

            var tenant = await EnsureTenantExistsAsync(seederContext, tenantSettings, services, ct);
            var adminUserId = await EnsureTenantAdminExistsAsync(seederContext, tenant, tenantSettings, services, ct);

            if (adminUserId != null)
            {
                tenants.Add((tenant, adminUserId));
            }
            else
            {
                logger.LogWarning("[SeedData] Could not resolve admin for tenant {Identifier}. Skipping.",
                    tenantSettings.Identifier);
            }
        }

        return tenants;
    }

    /// <summary>
    /// Ensures a tenant exists in the TenantStore. Creates it if missing.
    /// </summary>
    private static async Task<Tenant> EnsureTenantExistsAsync(
        SeederContext seederContext,
        SeedTenantSettings tenantSettings,
        IServiceProvider services,
        CancellationToken ct)
    {
        var logger = seederContext.Logger;
        var tenantStore = seederContext.TenantStoreContext;

        // Check if tenant already exists (bypass soft delete filter)
        var existingTenant = await tenantStore.TenantInfo
            .IgnoreQueryFilters()
            .TagWith("SeedData:GetExistingTenant")
            .FirstOrDefaultAsync(t => t.Identifier == tenantSettings.Identifier.ToLowerInvariant(), ct);

        if (existingTenant != null)
        {
            logger.LogInformation("[SeedData] Tenant already exists: {Identifier}", tenantSettings.Identifier);
            return existingTenant;
        }

        // Create new tenant
        var tenant = Tenant.Create(
            identifier: tenantSettings.Identifier,
            name: tenantSettings.Name,
            domain: tenantSettings.Domain,
            description: tenantSettings.Description,
            note: null,
            isActive: true);

        tenantStore.TenantInfo.Add(tenant);
        await tenantStore.SaveChangesAsync(ct);
        logger.LogInformation("[SeedData] Created tenant: {Identifier} ({Name})",
            tenantSettings.Identifier, tenantSettings.Name);

        return tenant;
    }

    /// <summary>
    /// Ensures the tenant has roles and an admin user. Returns admin userId.
    /// </summary>
    private static async Task<string?> EnsureTenantAdminExistsAsync(
        SeederContext seederContext,
        Tenant tenant,
        SeedTenantSettings tenantSettings,
        IServiceProvider services,
        CancellationToken ct)
    {
        var logger = seederContext.Logger;

        // Set Finbuckle context to this tenant so roles/users are scoped correctly
        SetTenantContext(tenant, services, logger);

        // Seed tenant roles (Admin, User) — idempotent
        var roleSeeder = new RoleSeeder();
        await roleSeeder.SeedTenantRolesAsync(seederContext, ct);

        // Create admin user for this tenant
        var adminSettings = new TenantAdminSettings
        {
            Email = tenantSettings.AdminEmail,
            Password = tenantSettings.AdminPassword,
            FirstName = tenantSettings.AdminFirstName,
            LastName = tenantSettings.AdminLastName,
            Enabled = true
        };

        await UserSeeder.SeedTenantAdminUserAsync(
            seederContext.UserManager,
            tenant.Id,
            adminSettings,
            logger);

        // Resolve the admin user ID
        var adminUser = await seederContext.UserManager.FindByEmailAsync(tenantSettings.AdminEmail);
        return adminUser?.Id;
    }

    /// <summary>
    /// Collects ISeedDataModule implementations based on enabled settings, sorted by Order.
    /// </summary>
    private static List<ISeedDataModule> GetEnabledModules(SeedDataSettings settings)
    {
        var modules = new List<ISeedDataModule>();

        if (settings.Modules.Catalog) modules.Add(new CatalogSeedModule());
        if (settings.Modules.Blog) modules.Add(new BlogSeedModule());
        if (settings.Modules.Commerce) modules.Add(new CommerceSeedModule());
        if (settings.Modules.Community) modules.Add(new CommunitySeedModule());
        if (settings.Modules.Engagement) modules.Add(new EngagementSeedModule());

        return modules.OrderBy(m => m.Order).ToList();
    }

    /// <summary>
    /// Sets the Finbuckle multi-tenant context to the specified tenant.
    /// </summary>
    private static void SetTenantContext(Tenant tenant, IServiceProvider services, ILogger logger)
    {
        var tenantSetter = services.GetService<IMultiTenantContextSetter>();
        if (tenantSetter != null)
        {
            tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(tenant);
            logger.LogDebug("[SeedData] Tenant context set to: {Identifier}", tenant.Identifier);
        }
    }
}
