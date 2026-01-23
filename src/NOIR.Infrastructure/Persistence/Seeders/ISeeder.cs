namespace NOIR.Infrastructure.Persistence.Seeders;

/// <summary>
/// Interface for database seeders that initialize required data.
/// Seeders are executed in a defined order to respect FK constraints.
/// </summary>
public interface ISeeder
{
    /// <summary>
    /// The order in which this seeder should run.
    /// Lower values run first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Seeds data into the database.
    /// Implementations must be idempotent - they should check if data exists before inserting.
    /// </summary>
    Task SeedAsync(SeederContext context, CancellationToken ct = default);
}

/// <summary>
/// Shared context passed to all seeders containing common dependencies.
/// </summary>
public class SeederContext
{
    public required ApplicationDbContext DbContext { get; init; }
    public required TenantStoreDbContext TenantStoreContext { get; init; }
    public required UserManager<ApplicationUser> UserManager { get; init; }
    public required RoleManager<ApplicationRole> RoleManager { get; init; }
    public required ILogger Logger { get; init; }
    public required IConfiguration Configuration { get; init; }
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Platform settings bound from configuration.
    /// </summary>
    public required PlatformSettings PlatformSettings { get; init; }

    /// <summary>
    /// The default tenant, set after tenant seeding.
    /// Available for subsequent seeders that need tenant context.
    /// </summary>
    public Tenant? DefaultTenant { get; set; }
}
