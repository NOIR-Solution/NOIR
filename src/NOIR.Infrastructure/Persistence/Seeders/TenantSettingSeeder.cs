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
}
