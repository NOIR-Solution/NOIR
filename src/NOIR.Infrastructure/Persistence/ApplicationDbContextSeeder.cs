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
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
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

        return existingTenant;
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

    internal static async Task SeedEmailTemplatesAsync(ApplicationDbContext context, ILogger logger)
    {
        // Check if templates already exist
        var existingTemplates = await context.Set<EmailTemplate>()
            .IgnoreQueryFilters()
            .Select(t => new { t.Name, t.Language })
            .ToListAsync();

        var existingKeys = existingTemplates
            .Select(t => $"{t.Name}:{t.Language}")
            .ToHashSet();

        var templatesToSeed = GetEmailTemplateDefinitions();
        var newTemplates = new List<EmailTemplate>();

        foreach (var template in templatesToSeed)
        {
            var key = $"{template.Name}:{template.Language}";
            if (!existingKeys.Contains(key))
            {
                newTemplates.Add(template);
                logger.LogInformation("Seeding email template: {Name} ({Language})", template.Name, template.Language);
            }
        }

        if (newTemplates.Count > 0)
        {
            await context.Set<EmailTemplate>().AddRangeAsync(newTemplates);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} email templates", newTemplates.Count);
        }
    }

    private static List<EmailTemplate> GetEmailTemplateDefinitions()
    {
        var templates = new List<EmailTemplate>();

        // Password Reset OTP - English
        templates.Add(EmailTemplate.Create(
            name: "PasswordResetOtp",
            subject: "Password Reset Code: {{OtpCode}}",
            htmlBody: GetPasswordResetOtpHtmlBody("en"),
            language: "en",
            plainTextBody: GetPasswordResetOtpPlainTextBody("en"),
            description: "Email sent when user requests password reset with OTP code.",
            availableVariables: "[\"UserName\", \"OtpCode\", \"ExpiryMinutes\"]"));

        // Password Reset OTP - Vietnamese
        templates.Add(EmailTemplate.Create(
            name: "PasswordResetOtp",
            subject: "Mã đặt lại mật khẩu: {{OtpCode}}",
            htmlBody: GetPasswordResetOtpHtmlBody("vi"),
            language: "vi",
            plainTextBody: GetPasswordResetOtpPlainTextBody("vi"),
            description: "Email gửi khi người dùng yêu cầu đặt lại mật khẩu với mã OTP.",
            availableVariables: "[\"UserName\", \"OtpCode\", \"ExpiryMinutes\"]"));

        // Welcome Email - English
        templates.Add(EmailTemplate.Create(
            name: "WelcomeEmail",
            subject: "Welcome to NOIR, {{UserName}}!",
            htmlBody: GetWelcomeEmailHtmlBody("en"),
            language: "en",
            plainTextBody: GetWelcomeEmailPlainTextBody("en"),
            description: "Email sent to new users after registration.",
            availableVariables: "[\"UserName\", \"LoginUrl\"]"));

        // Welcome Email - Vietnamese
        templates.Add(EmailTemplate.Create(
            name: "WelcomeEmail",
            subject: "Chào mừng đến với NOIR, {{UserName}}!",
            htmlBody: GetWelcomeEmailHtmlBody("vi"),
            language: "vi",
            plainTextBody: GetWelcomeEmailPlainTextBody("vi"),
            description: "Email gửi đến người dùng mới sau khi đăng ký.",
            availableVariables: "[\"UserName\", \"LoginUrl\"]"));

        // Account Activation - English
        templates.Add(EmailTemplate.Create(
            name: "AccountActivation",
            subject: "Activate Your Account",
            htmlBody: GetAccountActivationHtmlBody("en"),
            language: "en",
            plainTextBody: GetAccountActivationPlainTextBody("en"),
            description: "Email sent for account email verification.",
            availableVariables: "[\"UserName\", \"ActivationLink\", \"ExpiryHours\"]"));

        // Account Activation - Vietnamese
        templates.Add(EmailTemplate.Create(
            name: "AccountActivation",
            subject: "Kích hoạt tài khoản của bạn",
            htmlBody: GetAccountActivationHtmlBody("vi"),
            language: "vi",
            plainTextBody: GetAccountActivationPlainTextBody("vi"),
            description: "Email gửi để xác minh email tài khoản.",
            availableVariables: "[\"UserName\", \"ActivationLink\", \"ExpiryHours\"]"));

        return templates;
    }

    #region Email Template Content

    private static string GetPasswordResetOtpHtmlBody(string language) => language switch
    {
        "vi" => """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Đặt lại mật khẩu</title>
            </head>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
                <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                    <h1 style="color: white; margin: 0;">NOIR</h1>
                </div>
                <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                    <h2 style="color: #1e40af;">Xin chào {{UserName}},</h2>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu. Sử dụng mã OTP dưới đây để tiếp tục:</p>
                    <div style="background: #1e40af; color: white; padding: 20px; text-align: center; font-size: 32px; font-weight: bold; letter-spacing: 8px; border-radius: 8px; margin: 20px 0;">
                        {{OtpCode}}
                    </div>
                    <p style="color: #6b7280; font-size: 14px;">Mã này sẽ hết hạn sau <strong>{{ExpiryMinutes}} phút</strong>.</p>
                    <p style="color: #6b7280; font-size: 14px;">Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.</p>
                    <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                    <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 NOIR. Tất cả các quyền được bảo lưu.</p>
                </div>
            </body>
            </html>
            """,
        _ => """
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
            """
    };

    private static string GetPasswordResetOtpPlainTextBody(string language) => language switch
    {
        "vi" => """
            NOIR - Đặt lại mật khẩu

            Xin chào {{UserName}},

            Bạn đã yêu cầu đặt lại mật khẩu. Sử dụng mã OTP dưới đây:

            Mã OTP: {{OtpCode}}

            Mã này sẽ hết hạn sau {{ExpiryMinutes}} phút.

            Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này.

            © 2024 NOIR
            """,
        _ => """
            NOIR - Password Reset

            Hello {{UserName}},

            You have requested to reset your password. Use the OTP code below:

            OTP Code: {{OtpCode}}

            This code will expire in {{ExpiryMinutes}} minutes.

            If you did not request a password reset, please ignore this email.

            © 2024 NOIR
            """
    };

    private static string GetWelcomeEmailHtmlBody(string language) => language switch
    {
        "vi" => """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Chào mừng đến với NOIR</title>
            </head>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
                <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                    <h1 style="color: white; margin: 0;">NOIR</h1>
                </div>
                <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                    <h2 style="color: #1e40af;">Chào mừng, {{UserName}}!</h2>
                    <p>Cảm ơn bạn đã đăng ký tài khoản NOIR. Chúng tôi rất vui khi có bạn đồng hành!</p>
                    <p>Bạn có thể đăng nhập vào tài khoản của mình bằng nút dưới đây:</p>
                    <div style="text-align: center; margin: 30px 0;">
                        <a href="{{LoginUrl}}" style="background: #1e40af; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;">Đăng nhập ngay</a>
                    </div>
                    <p style="color: #6b7280; font-size: 14px;">Nếu bạn có bất kỳ câu hỏi nào, đừng ngần ngại liên hệ với đội hỗ trợ của chúng tôi.</p>
                    <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                    <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 NOIR. Tất cả các quyền được bảo lưu.</p>
                </div>
            </body>
            </html>
            """,
        _ => """
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
                    <p>Thank you for creating your NOIR account. We're excited to have you on board!</p>
                    <p>You can log in to your account using the button below:</p>
                    <div style="text-align: center; margin: 30px 0;">
                        <a href="{{LoginUrl}}" style="background: #1e40af; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;">Log In Now</a>
                    </div>
                    <p style="color: #6b7280; font-size: 14px;">If you have any questions, don't hesitate to reach out to our support team.</p>
                    <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                    <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 NOIR. All rights reserved.</p>
                </div>
            </body>
            </html>
            """
    };

    private static string GetWelcomeEmailPlainTextBody(string language) => language switch
    {
        "vi" => """
            NOIR - Chào mừng!

            Chào mừng, {{UserName}}!

            Cảm ơn bạn đã đăng ký tài khoản NOIR. Chúng tôi rất vui khi có bạn đồng hành!

            Đăng nhập tại: {{LoginUrl}}

            Nếu bạn có bất kỳ câu hỏi nào, đừng ngần ngại liên hệ với đội hỗ trợ của chúng tôi.

            © 2024 NOIR
            """,
        _ => """
            NOIR - Welcome!

            Welcome, {{UserName}}!

            Thank you for creating your NOIR account. We're excited to have you on board!

            Log in at: {{LoginUrl}}

            If you have any questions, don't hesitate to reach out to our support team.

            © 2024 NOIR
            """
    };

    private static string GetAccountActivationHtmlBody(string language) => language switch
    {
        "vi" => """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Kích hoạt tài khoản</title>
            </head>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
                <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                    <h1 style="color: white; margin: 0;">NOIR</h1>
                </div>
                <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                    <h2 style="color: #1e40af;">Xin chào {{UserName}},</h2>
                    <p>Vui lòng nhấp vào nút dưới đây để xác minh địa chỉ email và kích hoạt tài khoản của bạn:</p>
                    <div style="text-align: center; margin: 30px 0;">
                        <a href="{{ActivationLink}}" style="background: #1e40af; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;">Kích hoạt tài khoản</a>
                    </div>
                    <p style="color: #6b7280; font-size: 14px;">Liên kết này sẽ hết hạn sau <strong>{{ExpiryHours}} giờ</strong>.</p>
                    <p style="color: #6b7280; font-size: 14px;">Nếu bạn không tạo tài khoản này, vui lòng bỏ qua email này.</p>
                    <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                    <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 NOIR. Tất cả các quyền được bảo lưu.</p>
                </div>
            </body>
            </html>
            """,
        _ => """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Activate Your Account</title>
            </head>
            <body style="font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;">
                <div style="background: linear-gradient(135deg, #1e40af 0%, #0891b2 100%); padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                    <h1 style="color: white; margin: 0;">NOIR</h1>
                </div>
                <div style="background: #f9fafb; padding: 30px; border-radius: 0 0 10px 10px;">
                    <h2 style="color: #1e40af;">Hello {{UserName}},</h2>
                    <p>Please click the button below to verify your email address and activate your account:</p>
                    <div style="text-align: center; margin: 30px 0;">
                        <a href="{{ActivationLink}}" style="background: #1e40af; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;">Activate Account</a>
                    </div>
                    <p style="color: #6b7280; font-size: 14px;">This link will expire in <strong>{{ExpiryHours}} hours</strong>.</p>
                    <p style="color: #6b7280; font-size: 14px;">If you did not create this account, please ignore this email.</p>
                    <hr style="border: none; border-top: 1px solid #e5e7eb; margin: 30px 0;">
                    <p style="color: #9ca3af; font-size: 12px; text-align: center;">© 2024 NOIR. All rights reserved.</p>
                </div>
            </body>
            </html>
            """
    };

    private static string GetAccountActivationPlainTextBody(string language) => language switch
    {
        "vi" => """
            NOIR - Kích hoạt tài khoản

            Xin chào {{UserName}},

            Vui lòng truy cập liên kết dưới đây để xác minh địa chỉ email và kích hoạt tài khoản của bạn:

            {{ActivationLink}}

            Liên kết này sẽ hết hạn sau {{ExpiryHours}} giờ.

            Nếu bạn không tạo tài khoản này, vui lòng bỏ qua email này.

            © 2024 NOIR
            """,
        _ => """
            NOIR - Account Activation

            Hello {{UserName}},

            Please visit the link below to verify your email address and activate your account:

            {{ActivationLink}}

            This link will expire in {{ExpiryHours}} hours.

            If you did not create this account, please ignore this email.

            © 2024 NOIR
            """
    };

    #endregion
}
