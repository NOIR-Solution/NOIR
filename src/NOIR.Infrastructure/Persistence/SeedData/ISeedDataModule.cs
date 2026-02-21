namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Interface for seed data modules that populate demo/development data.
/// Each module handles a specific domain (Catalog, Blog, Commerce, Engagement).
/// Modules are executed in Order sequence per tenant.
/// </summary>
public interface ISeedDataModule
{
    /// <summary>
    /// Execution order. Lower values run first.
    /// Convention: 100=Catalog, 200=Blog, 300=Commerce, 400=Engagement.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Display name for logging (e.g., "Catalog", "Blog").
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// Seeds data for a single tenant. Implementations must be idempotent.
    /// </summary>
    Task SeedAsync(SeedDataContext context, CancellationToken ct = default);
}
