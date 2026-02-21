namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Per-tenant execution context passed to each ISeedDataModule.
/// Contains all dependencies needed to create seed entities for a single tenant.
/// </summary>
public class SeedDataContext
{
    /// <summary>
    /// The EF Core context scoped to the current tenant via Finbuckle.
    /// </summary>
    public required ApplicationDbContext DbContext { get; init; }

    /// <summary>
    /// Logger for seed progress and diagnostics.
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// Service provider for resolving additional services (e.g., IImageProcessor).
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Seed data settings from configuration.
    /// </summary>
    public required SeedDataSettings Settings { get; init; }

    /// <summary>
    /// The tenant being seeded.
    /// </summary>
    public required Tenant CurrentTenant { get; init; }

    /// <summary>
    /// The admin user ID for this tenant. Used for Post.AuthorId, Receipt.ConfirmedBy, etc.
    /// </summary>
    public required string TenantAdminUserId { get; init; }
}
