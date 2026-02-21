namespace NOIR.Infrastructure.Persistence.SeedData;

/// <summary>
/// Configuration POCO bound from "SeedData" appsettings section.
/// Controls which seed data modules are enabled and defines additional tenants.
/// </summary>
public class SeedDataSettings
{
    public const string SectionName = "SeedData";

    /// <summary>
    /// Master switch for seed data. Default false — must be explicitly enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Per-module toggle. All default to true when SeedData is enabled.
    /// </summary>
    public SeedDataModuleSettings Modules { get; set; } = new();

    /// <summary>
    /// Additional tenants to create beyond the default tenant.
    /// Each gets its own admin user and full seed data.
    /// </summary>
    public List<SeedTenantSettings> AdditionalTenants { get; set; } = [];
}

/// <summary>
/// Per-module enable/disable toggles.
/// </summary>
public class SeedDataModuleSettings
{
    public bool Catalog { get; set; } = true;
    public bool Blog { get; set; } = true;
    public bool Commerce { get; set; } = true;
    public bool Engagement { get; set; } = true;
}

/// <summary>
/// Settings for an additional tenant to create during seeding.
/// </summary>
public class SeedTenantSettings
{
    public string Identifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Domain { get; set; }
    public string? Description { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = "123qwe";
    public string AdminFirstName { get; set; } = string.Empty;
    public string AdminLastName { get; set; } = string.Empty;
}
