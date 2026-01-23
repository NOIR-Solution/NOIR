namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Fixes TenantId for notification preferences that were created before proper tenant context was set.
/// This handles the case where preferences exist but with a different TenantId than the current default tenant.
/// </summary>
public class NotificationPreferenceSeeder : ISeeder
{
    /// <summary>
    /// This should run after tenant context is established.
    /// </summary>
    public int Order => 100;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        await FixNotificationPreferencesTenantAsync(context.DbContext, context.Logger, ct);
    }

    /// <summary>
    /// Fixes TenantId for notification preferences created before proper tenant context was set.
    /// </summary>
    internal static async Task FixNotificationPreferencesTenantAsync(
        ApplicationDbContext context,
        ILogger logger,
        CancellationToken ct = default)
    {
        var currentTenantId = context.TenantInfo?.Id;
        if (string.IsNullOrEmpty(currentTenantId))
        {
            return;
        }

        // Get all preferences ignoring query filters to find those with wrong TenantId
        var allPreferences = await context.Set<NotificationPreference>()
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetAllNotificationPreferences")
            .Where(p => !p.IsDeleted)
            .ToListAsync(ct);

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
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Fixed TenantId for {Count} existing notification preferences", fixedCount);
        }
    }
}
