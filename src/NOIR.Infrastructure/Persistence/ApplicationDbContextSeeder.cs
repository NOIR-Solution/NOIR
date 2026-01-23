namespace NOIR.Infrastructure.Persistence;

/// <summary>
/// Initializes and seeds the database with required data.
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

            // Ensure database is created and migrations are applied FIRST
            // MigrateAsync is idempotent - it checks __EFMigrationsHistory and only applies pending migrations
            // EnsureCreatedAsync should ONLY be used for InMemory tests (no migration history tracking)
            if (context.Database.IsRelational())
            {
                // MigrateAsync handles all cases correctly:
                // 1. Database doesn't exist → Creates DB + applies all migrations + records in __EFMigrationsHistory
                // 2. Database exists, no migrations applied → Applies all pending migrations
                // 3. Database exists, some migrations applied → Applies only pending migrations
                // 4. Database exists, all migrations applied → Does nothing (idempotent)

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

            // === PHASE 1: Seed System-Level Roles (TenantId = null) ===
            // These roles are for platform-level administration
            await SeedSystemRolesAsync(roleManager, logger);

            // === PHASE 2: Seed Platform Admin User (TenantId = null) ===
            // This user can manage all tenants and platform-wide settings
            await SeedPlatformAdminUserAsync(userManager, platformSettings.PlatformAdmin, logger);

            // === PHASE 3: Seed Platform Email Templates (TenantId = null) ===
            // These templates are shared defaults that all tenants inherit from
            await SeedPlatformEmailTemplatesAsync(context, logger);

            // === PHASE 3b: Seed Platform Legal Pages (TenantId = null) ===
            // These are shared default legal pages (Terms, Privacy) that all tenants inherit
            await SeedPlatformLegalPagesAsync(context, logger);

            // === PHASE 3c: Seed Platform SMTP Settings (Mailhog defaults) ===
            await SeedPlatformSmtpSettingsAsync(context, logger);

            // === PHASE 4: Seed Default Tenant (if enabled) ===
            Tenant? defaultTenant = null;
            if (platformSettings.DefaultTenant.Enabled)
            {
                defaultTenant = await SeedDefaultTenantAsync(
                    tenantStoreContext,
                    platformSettings.DefaultTenant,
                    logger);

                // Set default tenant context for seeding tenant-specific data
                var tenantSetter = services.GetService<IMultiTenantContextSetter>();
                var tenantAccessor = services.GetService<IMultiTenantContextAccessor<Tenant>>();
                if (tenantSetter != null && tenantAccessor?.MultiTenantContext?.TenantInfo == null)
                {
                    logger.LogInformation(
                        "[Seeder] SETTING tenant context to default tenant: {TenantId} ({TenantName})",
                        defaultTenant.Id,
                        defaultTenant.Name);

                    // Set the seeded default tenant as the context
                    // Finbuckle v10 requires constructor argument for MultiTenantContext
                    tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(defaultTenant);

                    logger.LogInformation(
                        "[Seeder] Tenant context NOW SET. All subsequent saves will use TenantId: {TenantId}",
                        defaultTenant.Id);
                }

                // === PHASE 5: Seed Tenant-Level Roles ===
                // These roles are for within-tenant administration
                await SeedTenantRolesAsync(roleManager, logger);

                // === PHASE 6: Seed Default Tenant Admin (if enabled) ===
                if (platformSettings.DefaultTenant.Admin.Enabled)
                {
                    await SeedTenantAdminUserAsync(
                        userManager,
                        defaultTenant.Id,
                        platformSettings.DefaultTenant.Admin,
                        logger);
                }

                // Fix notification preferences TenantId (for preferences created before proper tenant context)
                await FixNotificationPreferencesTenantAsync(context, logger);
            }

            // Seed permissions (database-backed Permission entities)
            await SeedPermissionsAsync(context, logger);

            // Seed permission templates
            await SeedPermissionTemplatesAsync(context, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
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
                    isPlatformRole: definition.IsPlatformRole,  // Platform roles are hidden from tenant UI
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
    internal static async Task SeedTenantRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
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
    /// Seeds the default tenant in the database.
    /// This is required for Finbuckle EFCoreStore to resolve tenants.
    /// Uses TenantStoreDbContext which manages the Tenants table.
    /// </summary>
    internal static async Task<Tenant> SeedDefaultTenantAsync(
        TenantStoreDbContext context,
        DefaultTenantSettings settings,
        ILogger logger)
    {
        // Check if default tenant already exists (bypass soft delete filter)
        // TenantStoreDbContext inherits from EFCoreStoreDbContext<Tenant> which exposes TenantInfo as DbSet
        var existingTenant = await context.TenantInfo
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Identifier == settings.Identifier);

        if (existingTenant is null)
        {
            var tenant = Tenant.Create(
                identifier: settings.Identifier,
                name: settings.Name,
                domain: settings.Domain,
                description: settings.Description,
                note: null,
                isActive: true);

            context.TenantInfo.Add(tenant);
            await context.SaveChangesAsync();
            logger.LogInformation("Created default tenant: {Identifier} ({Name})", settings.Identifier, settings.Name);
            return tenant;
        }

        // Restore soft-deleted default tenant
        if (existingTenant.IsDeleted)
        {
            var restoredTenant = existingTenant with
            {
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null,
                IsActive = true,
                ModifiedAt = DateTimeOffset.UtcNow
            };

            context.TenantInfo.Entry(existingTenant).CurrentValues.SetValues(restoredTenant);
            await context.SaveChangesAsync();
            logger.LogInformation("Restored soft-deleted default tenant: {Identifier}", settings.Identifier);
            return restoredTenant;
        }

        // Update existing tenant with new settings if they've changed
        var needsUpdate = false;
        var updatedTenant = existingTenant;

        if (existingTenant.Name != settings.Name ||
            existingTenant.Domain != settings.Domain ||
            existingTenant.Description != settings.Description)
        {
            updatedTenant = existingTenant.WithUpdatedDetails(
                existingTenant.Identifier,
                settings.Name,
                settings.Domain,
                settings.Description,
                existingTenant.Note,
                existingTenant.IsActive);
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            context.TenantInfo.Entry(existingTenant).CurrentValues.SetValues(updatedTenant);
            await context.SaveChangesAsync();
            logger.LogInformation("Updated default tenant: {Identifier}", settings.Identifier);
            return updatedTenant;
        }

        return existingTenant;
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

    /// <summary>
    /// Seeds platform-level email templates (TenantId = null).
    /// These are shared defaults that all tenants inherit from.
    /// Tenants can customize these templates using the copy-on-edit pattern.
    /// </summary>
    internal static async Task SeedPlatformEmailTemplatesAsync(ApplicationDbContext context, ILogger logger)
    {
        // Platform email templates (TenantId = null) are shared defaults across all tenants.
        // Tenants inherit these templates and can create their own copies via copy-on-edit.
        //
        // Smart upsert logic for platform templates:
        // 1. If template doesn't exist at platform level → Add it
        // 2. If template exists AND Version = 1 → Update it (never customized)
        // 3. If template exists AND Version > 1 → Skip it (platform admin customized it)

        // Get existing platform-level templates (TenantId = null)
        var existingTemplates = await context.Set<EmailTemplate>()
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == null && !t.IsDeleted)
            .ToListAsync();

        var templateDefinitions = GetEmailTemplateDefinitions();
        var addedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        foreach (var definition in templateDefinitions)
        {
            var existing = existingTemplates.FirstOrDefault(t => t.Name == definition.Name);

            if (existing == null)
            {
                // Template doesn't exist at platform level - add it
                // Note: definition.TenantId is already null from GetEmailTemplateDefinitions()
                await context.Set<EmailTemplate>().AddAsync(definition);
                addedCount++;
            }
            else if (existing.Version == 1)
            {
                // Template exists but was never modified by user - update it
                existing.Update(
                    definition.Subject,
                    definition.HtmlBody,
                    definition.PlainTextBody,
                    definition.Description,
                    definition.AvailableVariables);

                // Reset version back to 1 since this is a seed update, not a user update
                ResetTemplateVersion(existing);
                updatedCount++;
            }
            else
            {
                // Template was customized by user (Version > 1) - skip it
                skippedCount++;
                logger.LogDebug(
                    "Skipping email template '{TemplateName}' - user customized (Version={Version})",
                    existing.Name, existing.Version);
            }
        }

        if (addedCount > 0 || updatedCount > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Platform email templates: {Added} added, {Updated} updated, {Skipped} skipped (customized)",
                addedCount, updatedCount, skippedCount);
        }
    }

    /// <summary>
    /// Resets template version back to 1 after a seed update.
    /// </summary>
    private static void ResetTemplateVersion(EmailTemplate template)
    {
        // Use the internal method instead of reflection
        template.ResetVersionForSeeding();
    }

    /// <summary>
    /// Seeds platform-level legal pages (TenantId = null).
    /// Tenants can customize these pages using the copy-on-edit pattern.
    /// </summary>
    internal static async Task SeedPlatformLegalPagesAsync(ApplicationDbContext context, ILogger logger)
    {
        // Platform legal pages (TenantId = null) are shared defaults across all tenants.
        // Tenants inherit these pages and can create their own copies via copy-on-edit.
        //
        // Smart upsert logic for platform pages:
        // 1. If page doesn't exist at platform level → Add it
        // 2. If page exists AND Version = 1 → Update it (never customized)
        // 3. If page exists AND Version > 1 → Skip it (platform admin customized it)

        // Get existing platform-level pages (TenantId = null)
        var existingPages = await context.Set<LegalPage>()
            .IgnoreQueryFilters()
            .Where(p => p.TenantId == null && !p.IsDeleted)
            .ToListAsync();

        var pageDefinitions = GetLegalPageDefinitions();
        var addedCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;

        foreach (var definition in pageDefinitions)
        {
            var existing = existingPages.FirstOrDefault(p => p.Slug == definition.Slug);

            if (existing == null)
            {
                // Page doesn't exist at platform level - add it
                await context.Set<LegalPage>().AddAsync(definition);
                addedCount++;
            }
            else if (existing.Version == 1)
            {
                // Page exists but was never modified by user - update it
                existing.Update(
                    definition.Title,
                    definition.HtmlContent,
                    metaTitle: definition.MetaTitle,
                    metaDescription: definition.MetaDescription,
                    canonicalUrl: definition.CanonicalUrl,
                    allowIndexing: definition.AllowIndexing);

                // Reset version back to 1 since this is a seed update, not a user update
                existing.ResetVersionForSeeding();
                updatedCount++;
            }
            else
            {
                // Page was customized by user (Version > 1) - skip it
                skippedCount++;
                logger.LogDebug(
                    "Skipping legal page '{PageSlug}' - user customized (Version={Version})",
                    existing.Slug, existing.Version);
            }
        }

        if (addedCount > 0 || updatedCount > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "Platform legal pages: {Added} added, {Updated} updated, {Skipped} skipped (customized)",
                addedCount, updatedCount, skippedCount);
        }
    }

    /// <summary>
    /// Gets the legal page definitions for seeding.
    /// </summary>
    private static List<LegalPage> GetLegalPageDefinitions()
    {
        return
        [
            LegalPage.CreatePlatformDefault(
                "terms-of-service",
                "Terms of Service",
                GetTermsOfServiceHtmlContent(),
                metaTitle: "Terms of Service",
                metaDescription: "Our terms and conditions for using this platform."),
            LegalPage.CreatePlatformDefault(
                "privacy-policy",
                "Privacy Policy",
                GetPrivacyPolicyHtmlContent(),
                metaTitle: "Privacy Policy",
                metaDescription: "How we collect, use, and protect your personal information."),
        ];
    }

    /// <summary>
    /// Gets the default Terms of Service HTML content.
    /// </summary>
    private static string GetTermsOfServiceHtmlContent()
    {
        return """
            <h1>Terms of Service</h1>

            <p><strong>Last Updated:</strong> {{CurrentDate}}</p>

            <h2>1. Acceptance of Terms</h2>
            <p>By accessing and using this platform, you accept and agree to be bound by the terms and provision of this agreement. If you do not agree to abide by the above, please do not use this service.</p>

            <h2>2. Use License</h2>
            <p>Permission is granted to temporarily access the materials (information or software) on this platform for personal, non-commercial transitory viewing only. This is the grant of a license, not a transfer of title.</p>

            <h2>3. User Responsibilities</h2>
            <p>Users are responsible for:</p>
            <ul>
                <li>Maintaining the confidentiality of their account credentials</li>
                <li>All activities that occur under their account</li>
                <li>Complying with all applicable laws and regulations</li>
                <li>Respecting the rights and privacy of other users</li>
            </ul>

            <h2>4. Prohibited Activities</h2>
            <p>You may not:</p>
            <ul>
                <li>Use the service for any unlawful purpose</li>
                <li>Attempt to gain unauthorized access to any portion of the platform</li>
                <li>Interfere with or disrupt the service</li>
                <li>Upload or transmit viruses or malicious code</li>
            </ul>

            <h2>5. Disclaimer</h2>
            <p>The materials on this platform are provided on an 'as is' basis. We make no warranties, expressed or implied, and hereby disclaim and negate all other warranties including, without limitation, implied warranties or conditions of merchantability, fitness for a particular purpose, or non-infringement of intellectual property or other violation of rights.</p>

            <h2>6. Limitations</h2>
            <p>In no event shall we or our suppliers be liable for any damages (including, without limitation, damages for loss of data or profit, or due to business interruption) arising out of the use or inability to use the materials on this platform.</p>

            <h2>7. Changes to Terms</h2>
            <p>We reserve the right to modify these terms at any time. Continued use of the platform after any such changes shall constitute your consent to such changes.</p>

            <h2>8. Contact</h2>
            <p>If you have any questions about these Terms of Service, please contact us.</p>
            """;
    }

    /// <summary>
    /// Gets the default Privacy Policy HTML content.
    /// </summary>
    private static string GetPrivacyPolicyHtmlContent()
    {
        return """
            <h1>Privacy Policy</h1>

            <p><strong>Last Updated:</strong> {{CurrentDate}}</p>

            <h2>1. Introduction</h2>
            <p>We respect your privacy and are committed to protecting your personal data. This privacy policy will inform you about how we look after your personal data and tell you about your privacy rights.</p>

            <h2>2. Information We Collect</h2>
            <p>We may collect and process the following data:</p>
            <ul>
                <li><strong>Identity Data:</strong> Name, username, and similar identifiers</li>
                <li><strong>Contact Data:</strong> Email address and telephone numbers</li>
                <li><strong>Technical Data:</strong> IP address, browser type, time zone settings, and device information</li>
                <li><strong>Usage Data:</strong> Information about how you use our platform</li>
            </ul>

            <h2>3. How We Use Your Information</h2>
            <p>We use your personal data for:</p>
            <ul>
                <li>Providing and managing your account</li>
                <li>Delivering our services to you</li>
                <li>Communicating with you about our services</li>
                <li>Improving our platform and user experience</li>
                <li>Complying with legal obligations</li>
            </ul>

            <h2>4. Data Security</h2>
            <p>We have implemented appropriate security measures to prevent your personal data from being accidentally lost, used, or accessed in an unauthorized way. We limit access to your personal data to those employees and partners who have a business need to know.</p>

            <h2>5. Data Retention</h2>
            <p>We will only retain your personal data for as long as necessary to fulfil the purposes we collected it for, including for the purposes of satisfying any legal, accounting, or reporting requirements.</p>

            <h2>6. Your Rights</h2>
            <p>Under certain circumstances, you have rights under data protection laws in relation to your personal data, including:</p>
            <ul>
                <li>The right to access your personal data</li>
                <li>The right to request correction of your personal data</li>
                <li>The right to request erasure of your personal data</li>
                <li>The right to object to processing of your personal data</li>
                <li>The right to request transfer of your personal data</li>
            </ul>

            <h2>7. Cookies</h2>
            <p>Our platform uses cookies to distinguish you from other users. This helps us provide you with a good experience when you browse our platform and allows us to improve our site.</p>

            <h2>8. Third-Party Links</h2>
            <p>This platform may include links to third-party websites. Clicking on those links may allow third parties to collect or share data about you. We do not control these third-party websites and are not responsible for their privacy statements.</p>

            <h2>9. Changes to This Policy</h2>
            <p>We may update this privacy policy from time to time. We will notify you of any changes by posting the new privacy policy on this page and updating the "Last Updated" date.</p>

            <h2>10. Contact Us</h2>
            <p>If you have any questions about this privacy policy or our privacy practices, please contact us.</p>
            """;
    }

    /// <summary>
    /// Seeds platform-level SMTP settings with Mailhog defaults for local development.
    /// Uses smart upsert: only seeds if no SMTP settings exist yet.
    /// </summary>
    internal static async Task SeedPlatformSmtpSettingsAsync(ApplicationDbContext context, ILogger logger)
    {
        // Check if any SMTP settings already exist at platform level
        var existingSmtpSettings = await context.Set<TenantSetting>()
            .IgnoreQueryFilters()
            .Where(s => s.TenantId == null && s.Key.StartsWith("smtp:") && !s.IsDeleted)
            .ToListAsync();

        if (existingSmtpSettings.Count > 0)
        {
            logger.LogDebug("Platform SMTP settings already exist ({Count} keys), skipping seed", existingSmtpSettings.Count);
            return;
        }

        // Seed Mailhog defaults for local development
        var smtpSettings = new List<TenantSetting>
        {
            TenantSetting.CreatePlatformDefault("smtp:host", "localhost", "string", "SMTP server hostname", "smtp"),
            TenantSetting.CreatePlatformDefault("smtp:port", "1025", "int", "SMTP server port", "smtp"),
            TenantSetting.CreatePlatformDefault("smtp:from_email", "noreply@noir.local", "string", "Default sender email address", "smtp"),
            TenantSetting.CreatePlatformDefault("smtp:from_name", "NOIR Platform", "string", "Default sender display name", "smtp"),
            TenantSetting.CreatePlatformDefault("smtp:use_ssl", "false", "bool", "Whether to use SSL/TLS", "smtp"),
        };

        await context.Set<TenantSetting>().AddRangeAsync(smtpSettings);
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} platform SMTP settings (Mailhog defaults)", smtpSettings.Count);
    }

    /// <summary>
    /// Fixes TenantId for notification preferences that were created before proper tenant context was set.
    /// This handles the case where preferences exist but with a different TenantId than the current default tenant.
    /// </summary>
    internal static async Task FixNotificationPreferencesTenantAsync(ApplicationDbContext context, ILogger logger)
    {
        var currentTenantId = context.TenantInfo?.Id;
        if (string.IsNullOrEmpty(currentTenantId))
        {
            return;
        }

        // Get all preferences ignoring query filters to find those with wrong TenantId
        var allPreferences = await context.Set<NotificationPreference>()
            .IgnoreQueryFilters()
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        var fixedCount = 0;
        foreach (var preference in allPreferences)
        {
            if (preference.TenantId != currentTenantId)
            {
                var entry = context.Entry(preference);
                entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = currentTenantId;
                entry.Property(p => p.ModifiedAt).CurrentValue = DateTimeOffset.UtcNow;
                fixedCount++;
            }
        }

        if (fixedCount > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Fixed TenantId for {Count} existing notification preferences", fixedCount);
        }
    }

    /// <summary>
    /// Seeds Permission entities based on the Permissions constants.
    /// These enable database-backed permission management alongside claims.
    /// </summary>
    internal static async Task SeedPermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        var existingPermissions = await context.Set<Permission>()
            .IgnoreQueryFilters()
            .ToListAsync();

        var existingByName = existingPermissions.ToDictionary(p => p.Name);
        var permissionsToSeed = GetPermissionDefinitions();
        var newPermissions = new List<Permission>();

        foreach (var permission in permissionsToSeed)
        {
            if (!existingByName.ContainsKey(permission.Name))
            {
                newPermissions.Add(permission);
                logger.LogInformation("Seeding permission: {Permission}", permission.Name);
            }
        }

        if (newPermissions.Count > 0)
        {
            await context.Set<Permission>().AddRangeAsync(newPermissions);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} permissions", newPermissions.Count);
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

    /// <summary>
    /// Seeds PermissionTemplate entities with predefined role permission sets.
    /// </summary>
    internal static async Task SeedPermissionTemplatesAsync(ApplicationDbContext context, ILogger logger)
    {
        var existingTemplates = await context.Set<PermissionTemplate>()
            .IgnoreQueryFilters()
            .Include(t => t.Items)
            .ToListAsync();

        var existingByName = existingTemplates.ToDictionary(t => t.Name);

        // Get all permissions from DB for linking
        var allPermissions = await context.Set<Permission>()
            .IgnoreQueryFilters()
            .ToListAsync();
        var permissionsByName = allPermissions.ToDictionary(p => p.Name);

        var templatesToSeed = GetPermissionTemplateDefinitions();
        var newTemplates = new List<PermissionTemplate>();

        foreach (var (templateName, templateDef) in templatesToSeed)
        {
            if (!existingByName.ContainsKey(templateName))
            {
                var template = PermissionTemplate.Create(
                    templateName,
                    templateDef.Description,
                    tenantId: null,
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
                        logger.LogWarning("Permission {Permission} not found for template {Template}", permissionName, templateName);
                    }
                }

                newTemplates.Add(template);
                logger.LogInformation("Seeding permission template: {Template} with {Count} permissions", templateName, templateDef.Permissions.Count);
            }
        }

        if (newTemplates.Count > 0)
        {
            await context.Set<PermissionTemplate>().AddRangeAsync(newTemplates);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} permission templates", newTemplates.Count);
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

    private static List<EmailTemplate> GetEmailTemplateDefinitions()
    {
        var templates = new List<EmailTemplate>();

        // Password Reset OTP
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "PasswordResetOtp",
            subject: "Password Reset Code: {{OtpCode}}",
            htmlBody: GetPasswordResetOtpHtmlBody(),
            plainTextBody: GetPasswordResetOtpPlainTextBody(),
            description: "Email sent when user requests password reset with OTP code.",
            availableVariables: "[\"UserName\", \"OtpCode\", \"ExpiryMinutes\"]"));

        // Email Change OTP
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "EmailChangeOtp",
            subject: "Email Change Verification Code: {{OtpCode}}",
            htmlBody: GetEmailChangeOtpHtmlBody(),
            plainTextBody: GetEmailChangeOtpPlainTextBody(),
            description: "Email sent when user requests to change their email address with OTP code.",
            availableVariables: "[\"UserName\", \"OtpCode\", \"ExpiryMinutes\"]"));

        // Welcome Email (used when admin creates user)
        templates.Add(EmailTemplate.CreatePlatformDefault(
            name: "WelcomeEmail",
            subject: "Welcome to NOIR - Your Account Has Been Created",
            htmlBody: GetWelcomeEmailHtmlBody(),
            plainTextBody: GetWelcomeEmailPlainTextBody(),
            description: "Email sent to users when their account is created by an administrator.",
            availableVariables: "[\"UserName\", \"Email\", \"TemporaryPassword\", \"LoginUrl\", \"ApplicationName\"]"));

        return templates;
    }

    #region Email Template Content

    private static string GetPasswordResetOtpHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Password Reset</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">NOIR</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{UserName}},</h2>
                <p>You have requested to reset your password. Use the OTP code below to continue:</p>
                <div style="background: #1e40af; color: white; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; border-radius: 8px; margin: 20px 0;">
                    {{OtpCode}}
                </div>
                <p style="color: #6b7280; font-size: 14px;">This code will expire in <strong>{{ExpiryMinutes}} minutes</strong>.</p>
                <p style="color: #6b7280; font-size: 14px;">If you did not request a password reset, please ignore this email.</p>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 NOIR. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetPasswordResetOtpPlainTextBody() => """
        NOIR - Password Reset

        Hello {{UserName}},

        You have requested to reset your password. Use the OTP code below:

        OTP Code: {{OtpCode}}

        This code will expire in {{ExpiryMinutes}} minutes.

        If you did not request a password reset, please ignore this email.

        © 2024 NOIR
        """;

    private static string GetEmailChangeOtpHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Email Change Verification</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">NOIR</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Hello {{UserName}},</h2>
                <p>You have requested to change your email address. Use the OTP code below to verify your new email:</p>
                <div style="background: #1e40af; color: white; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; border-radius: 8px; margin: 20px 0;">
                    {{OtpCode}}
                </div>
                <p style="color: #6b7280; font-size: 14px;">This code will expire in <strong>{{ExpiryMinutes}} minutes</strong>.</p>
                <p style="color: #6b7280; font-size: 14px;">If you did not request an email change, please ignore this email and your email address will remain unchanged.</p>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 NOIR. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetEmailChangeOtpPlainTextBody() => """
        NOIR - Email Change Verification

        Hello {{UserName}},

        You have requested to change your email address. Use the OTP code below to verify your new email:

        OTP Code: {{OtpCode}}

        This code will expire in {{ExpiryMinutes}} minutes.

        If you did not request an email change, please ignore this email and your email address will remain unchanged.

        © 2024 NOIR
        """;

    private static string GetWelcomeEmailHtmlBody() => """
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Welcome to NOIR</title>
        </head>
        <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
            <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                <h1 style="color: white; margin: 0;">NOIR</h1>
            </div>
            <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                <h2 style="color: #1e40af;">Welcome, {{UserName}}!</h2>
                <p>An administrator has created an account for you in <strong>{{ApplicationName}}</strong>.</p>
                <p>Here are your login credentials:</p>
                <div style="background: #f1f5f9; padding: 15px; border-radius: 8px; margin: 15px 0;">
                    <p style="margin: 5px 0;"><strong>Email:</strong> {{Email}}</p>
                </div>
                <p style="margin-bottom: 5px;"><strong>Your temporary password:</strong></p>
                <div style="background: #1e40af; color: white; padding: 20px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 4px; border-radius: 8px; margin: 10px 0 20px 0; font-family: monospace;">
                    {{TemporaryPassword}}
                </div>
                <div style="background: #fef3c7; border-left: 4px solid #f59e0b; padding: 12px 15px; margin: 20px 0; border-radius: 0 8px 8px 0;">
                    <p style="margin: 0; color: #92400e; font-size: 14px;"><strong>⚠ Important:</strong> Please change your password immediately after your first login.</p>
                </div>
                <div style="text-align: center; margin: 25px 0;">
                    <a href="{{LoginUrl}}" style="display: inline-block; background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); color: white; text-decoration: none; padding: 14px 35px; border-radius: 8px; font-weight: bold;">Log In Now</a>
                </div>
                <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 {{ApplicationName}}. All rights reserved.</p>
            </div>
        </body>
        </html>
        """;

    private static string GetWelcomeEmailPlainTextBody() => """
        {{ApplicationName}} - Welcome!

        Hello {{UserName}},

        An administrator has created an account for you in {{ApplicationName}}.

        Email: {{Email}}
        Temporary Password: {{TemporaryPassword}}

        ⚠️ IMPORTANT: Please change your password immediately after your first login.

        Log in at: {{LoginUrl}}

        If you have any questions, please contact your administrator.

        © 2024 {{ApplicationName}}
        """;

    #endregion
}
