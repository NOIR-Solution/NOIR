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
            var tenantStoreContext = services.GetRequiredService<TenantStoreDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

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

            // Seed default tenant (required for Finbuckle EFCoreStore and multi-tenant query filters)
            // This creates the default tenant in the database if it doesn't exist
            var defaultTenant = await SeedDefaultTenantAsync(tenantStoreContext, logger);

            // Set default tenant context for seeding other data (required for multi-tenant query filters)
            var tenantSetter = services.GetService<IMultiTenantContextSetter>();
            var tenantAccessor = services.GetService<IMultiTenantContextAccessor<Tenant>>();
            if (tenantSetter != null && tenantAccessor?.MultiTenantContext?.TenantInfo == null)
            {
                // Set the seeded default tenant as the context
                // Finbuckle v10 requires constructor argument for MultiTenantContext
                tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(defaultTenant);
            }

            // Seed roles with permissions
            await SeedRolesAsync(roleManager, logger);

            // Seed admin user
            await SeedAdminUserAsync(userManager, logger);

            // Seed email templates
            await SeedEmailTemplatesAsync(context, logger);

            // Fix notification preferences TenantId (for preferences created before proper tenant context)
            await FixNotificationPreferencesTenantAsync(context, logger);

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
    /// Seeds the default tenant in the database.
    /// This is required for Finbuckle EFCoreStore to resolve tenants.
    /// Uses TenantStoreDbContext which manages the Tenants table.
    /// </summary>
    internal static async Task<Tenant> SeedDefaultTenantAsync(TenantStoreDbContext context, ILogger logger)
    {
        const string defaultIdentifier = "default";

        // Check if default tenant already exists (bypass soft delete filter)
        // TenantStoreDbContext inherits from EFCoreStoreDbContext<Tenant> which exposes TenantInfo as DbSet
        var existingTenant = await context.TenantInfo
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Identifier == defaultIdentifier);

        if (existingTenant is null)
        {
            var tenant = Tenant.Create(
                identifier: defaultIdentifier,
                name: "Default Tenant",
                isActive: true);

            context.TenantInfo.Add(tenant);
            await context.SaveChangesAsync();
            logger.LogInformation("Created default tenant: {Identifier}", defaultIdentifier);
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
            logger.LogInformation("Restored soft-deleted default tenant: {Identifier}", defaultIdentifier);
            return restoredTenant;
        }

        return existingTenant;
    }

    internal static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ILogger logger)
    {
        // Define role hierarchy and properties
        var roleDefinitions = new Dictionary<string, (string? Description, string? ParentRoleId, int SortOrder, string? IconName, string? Color)>
        {
            [Roles.Admin] = ("Full system access with all permissions", null, 0, "shield", "red"),
            [Roles.User] = ("Standard user access", null, 10, "user", "blue")
        };

        foreach (var roleName in Roles.Defaults)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            var definition = roleDefinitions.GetValueOrDefault(roleName);

            if (role is null)
            {
                role = ApplicationRole.Create(
                    roleName,
                    definition.Description,
                    definition.ParentRoleId,
                    tenantId: null,
                    isSystemRole: true,
                    definition.SortOrder,
                    definition.IconName,
                    definition.Color);
                await roleManager.CreateAsync(role);
                logger.LogInformation("Created role: {Role}", roleName);
            }
            else
            {
                // Update existing role to be a system role with proper properties
                if (!role.IsSystemRole)
                {
                    role.Update(
                        roleName,
                        definition.Description,
                        definition.ParentRoleId,
                        definition.SortOrder,
                        definition.IconName,
                        definition.Color);
                    // Mark as system role directly
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
                IsSystemUser = true,
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
            // Ensure admin is marked as system user (for upgrades from older versions)
            if (!adminUser.IsSystemUser)
            {
                adminUser.IsSystemUser = true;
                await userManager.UpdateAsync(adminUser);
                logger.LogInformation("Marked admin user as system user: {Email}", adminEmail);
            }

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

    internal static async Task SeedEmailTemplatesAsync(ApplicationDbContext context, ILogger logger)
    {
        // Email templates are platform-level (TenantId = null) shared across all tenants.
        // Tenants can create their own templates to override platform defaults.
        var existingNames = await context.Set<EmailTemplate>()
            .IgnoreQueryFilters()
            .Select(t => t.Name)
            .ToListAsync();

        var templatesToSeed = GetEmailTemplateDefinitions()
            .Where(t => !existingNames.Contains(t.Name))
            .ToList();

        if (templatesToSeed.Count > 0)
        {
            await context.Set<EmailTemplate>().AddRangeAsync(templatesToSeed);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} email templates", templatesToSeed.Count);
        }
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
        templates.Add(EmailTemplate.Create(
            name: "PasswordResetOtp",
            subject: "Password Reset Code: {{OtpCode}}",
            htmlBody: GetPasswordResetOtpHtmlBody(),
            plainTextBody: GetPasswordResetOtpPlainTextBody(),
            description: "Email sent when user requests password reset with OTP code.",
            availableVariables: "[\"UserName\", \"OtpCode\", \"ExpiryMinutes\"]"));

        // Welcome Email (used when admin creates user)
        templates.Add(EmailTemplate.Create(
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
