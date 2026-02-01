namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Seeds the default tenant in the database.
/// This is required for Finbuckle EFCoreStore to resolve tenants.
/// </summary>
public class TenantSeeder : ISeeder
{
    /// <summary>
    /// Tenants should be seeded early in the process.
    /// </summary>
    public int Order => 50;

    public async Task SeedAsync(SeederContext context, CancellationToken ct = default)
    {
        if (!context.PlatformSettings.DefaultTenant.Enabled)
        {
            return;
        }

        var defaultTenant = await SeedDefaultTenantAsync(
            context.TenantStoreContext,
            context.PlatformSettings.DefaultTenant,
            context.Logger,
            ct);

        // Store the tenant in context for subsequent seeders
        context.DefaultTenant = defaultTenant;
    }

    /// <summary>
    /// Seeds the default tenant in the database.
    /// Uses TenantStoreDbContext which manages the Tenants table.
    /// </summary>
    internal static async Task<Tenant> SeedDefaultTenantAsync(
        TenantStoreDbContext context,
        DefaultTenantSettings settings,
        ILogger logger,
        CancellationToken ct = default)
    {
        // Check if default tenant already exists (bypass soft delete filter)
        var existingTenant = await context.TenantInfo
            .IgnoreQueryFilters()
            .TagWith("Seeder:GetExistingTenant")
            .FirstOrDefaultAsync(t => t.Identifier == settings.Identifier, ct);

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
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Created default tenant: {Identifier} ({Name})", settings.Identifier, settings.Name);
            return tenant;
        }

        // Restore soft-deleted default tenant using immutable pattern
        if (existingTenant.IsDeleted)
        {
            var restoredTenant = existingTenant.CreateRestored();
            context.TenantInfo.Entry(existingTenant).CurrentValues.SetValues(restoredTenant);
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Restored soft-deleted default tenant: {Identifier}", settings.Identifier);
            return restoredTenant;
        }

        // Update existing tenant with new settings if they've changed
        if (existingTenant.Name != settings.Name ||
            existingTenant.Domain != settings.Domain ||
            existingTenant.Description != settings.Description)
        {
            var updatedTenant = existingTenant.CreateUpdated(
                existingTenant.Identifier!,
                settings.Name,
                settings.Domain,
                settings.Description,
                existingTenant.Note,
                existingTenant.IsActive);

            context.TenantInfo.Entry(existingTenant).CurrentValues.SetValues(updatedTenant);
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Updated default tenant: {Identifier}", settings.Identifier);
            return updatedTenant;
        }

        return existingTenant;
    }
}
