namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds platform-level tenant settings including SMTP configuration.
/// Uses Mailhog defaults for local development.
/// </summary>
public class TenantSettingSeeder : ISeeder
{
    /// <summary>
    /// Tenant settings can be seeded after basic system setup.
    /// </summary>
    public int Order => 46;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        await SeedPlatformSmtpSettingsAsync(context.DbContext, context.Logger, ct);
        await SeedPlatformBrandingSettingsAsync(context.DbContext, context.Logger, ct);
        await SeedPlatformContactSettingsAsync(context.DbContext, context.Logger, ct);
        await SeedPlatformRegionalSettingsAsync(context.DbContext, context.Logger, ct);
        await SeedPlatformPaymentSettingsAsync(context.DbContext, context.Logger, ct);
    }

    /// <summary>
    /// Seeds platform-level SMTP settings with Mailhog defaults for local development.
    /// Uses smart upsert: only seeds if no SMTP settings exist yet.
    /// </summary>
    internal static async Task SeedPlatformSmtpSettingsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken ct = default)
    {
        // Check if any SMTP settings already exist at platform level
        var existingSmtpSettings = await context.Set<TenantSetting>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetPlatformSmtpSettings")
            .Where(s => s.TenantId == null && s.Key.StartsWith("smtp:") && !s.IsDeleted)
            .ToListAsync(ct);

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

        await context.Set<TenantSetting>().AddRangeAsync(smtpSettings, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} platform SMTP settings (Mailhog defaults)", smtpSettings.Count);
    }

    internal static async Task SeedPlatformBrandingSettingsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken ct = default)
    {
        var existingSettings = await context.Set<TenantSetting>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetPlatformBrandingSettings")
            .Where(s => s.TenantId == null && s.Key.StartsWith("branding:") && !s.IsDeleted)
            .ToListAsync(ct);

        if (existingSettings.Count > 0)
        {
            logger.LogDebug("Platform branding settings already exist ({Count} keys), skipping seed", existingSettings.Count);
            return;
        }

        var settings = new List<TenantSetting>
        {
            TenantSetting.CreatePlatformDefault("branding:company_name", "NOIR Fashion", "string", "Company display name", "branding"),
            TenantSetting.CreatePlatformDefault("branding:primary_color", "#1F2937", "string", "Primary brand color (hex)", "branding"),
            TenantSetting.CreatePlatformDefault("branding:secondary_color", "#6366F1", "string", "Secondary brand color (hex)", "branding"),
            TenantSetting.CreatePlatformDefault("branding:accent_color", "#F59E0B", "string", "Accent color (hex)", "branding"),
            TenantSetting.CreatePlatformDefault("branding:dark_mode_default", "false", "bool", "Whether dark mode is enabled by default", "branding"),
        };

        await context.Set<TenantSetting>().AddRangeAsync(settings, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} platform branding settings", settings.Count);
    }

    internal static async Task SeedPlatformContactSettingsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken ct = default)
    {
        var existingSettings = await context.Set<TenantSetting>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetPlatformContactSettings")
            .Where(s => s.TenantId == null && s.Key.StartsWith("contact:") && !s.IsDeleted)
            .ToListAsync(ct);

        if (existingSettings.Count > 0)
        {
            logger.LogDebug("Platform contact settings already exist ({Count} keys), skipping seed", existingSettings.Count);
            return;
        }

        var settings = new List<TenantSetting>
        {
            TenantSetting.CreatePlatformDefault("contact:email", "contact@noir.local", "string", "Primary contact email address", "contact"),
            TenantSetting.CreatePlatformDefault("contact:phone", "+84 28 1234 5678", "string", "Primary contact phone number", "contact"),
            TenantSetting.CreatePlatformDefault("contact:address", "123 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh", "string", "Business address", "contact"),
            TenantSetting.CreatePlatformDefault("contact:support_email", "support@noir.local", "string", "Customer support email address", "contact"),
            TenantSetting.CreatePlatformDefault("contact:facebook_url", "https://facebook.com/noir.fashion", "string", "Facebook page URL", "contact"),
        };

        await context.Set<TenantSetting>().AddRangeAsync(settings, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} platform contact settings", settings.Count);
    }

    internal static async Task SeedPlatformRegionalSettingsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken ct = default)
    {
        var existingSettings = await context.Set<TenantSetting>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetPlatformRegionalSettings")
            .Where(s => s.TenantId == null && s.Key.StartsWith("regional:") && !s.IsDeleted)
            .ToListAsync(ct);

        if (existingSettings.Count > 0)
        {
            logger.LogDebug("Platform regional settings already exist ({Count} keys), skipping seed", existingSettings.Count);
            return;
        }

        var settings = new List<TenantSetting>
        {
            TenantSetting.CreatePlatformDefault("regional:timezone", "Asia/Ho_Chi_Minh", "string", "Default timezone (IANA)", "regional"),
            TenantSetting.CreatePlatformDefault("regional:currency", "VND", "string", "Default currency code (ISO 4217)", "regional"),
            TenantSetting.CreatePlatformDefault("regional:language", "vi", "string", "Default language code (ISO 639-1)", "regional"),
            TenantSetting.CreatePlatformDefault("regional:date_format", "dd/MM/yyyy", "string", "Default date display format", "regional"),
            TenantSetting.CreatePlatformDefault("regional:country", "VN", "string", "Default country code (ISO 3166-1)", "regional"),
        };

        await context.Set<TenantSetting>().AddRangeAsync(settings, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} platform regional settings", settings.Count);
    }

    internal static async Task SeedPlatformPaymentSettingsAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken ct = default)
    {
        var existingSettings = await context.Set<TenantSetting>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetPlatformPaymentSettings")
            .Where(s => s.TenantId == null && s.Key.StartsWith("payment:") && !s.IsDeleted)
            .ToListAsync(ct);

        if (existingSettings.Count > 0)
        {
            logger.LogDebug("Platform payment settings already exist ({Count} keys), skipping seed", existingSettings.Count);
            return;
        }

        var settings = new List<TenantSetting>
        {
            TenantSetting.CreatePlatformDefault("payment:cod_enabled", "true", "bool", "Whether Cash on Delivery is enabled", "payment"),
            TenantSetting.CreatePlatformDefault("payment:cod_min_amount", "0", "decimal", "Minimum order amount for COD (VND)", "payment"),
            TenantSetting.CreatePlatformDefault("payment:cod_max_amount", "20000000", "decimal", "Maximum order amount for COD (VND)", "payment"),
            TenantSetting.CreatePlatformDefault("payment:cod_description", "Thanh toán khi nhận hàng", "string", "COD payment method description", "payment"),
        };

        await context.Set<TenantSetting>().AddRangeAsync(settings, ct);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} platform payment settings", settings.Count);
    }
}
