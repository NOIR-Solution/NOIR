namespace NOIR.Application.Common.Settings;

public class PlatformSettings
{
    public const string SectionName = "Platform";
    public PlatformAdminSettings PlatformAdmin { get; set; } = new();
    public DefaultTenantSettings DefaultTenant { get; set; } = new();
}

public class PlatformAdminSettings
{
    public string Email { get; set; } = "platform@noir.local";
    public string Password { get; set; } = "123qwe";
    public string FirstName { get; set; } = "Platform";
    public string LastName { get; set; } = "Administrator";
}

public class DefaultTenantSettings
{
    public bool Enabled { get; set; } = true;
    public string Identifier { get; set; } = "default";
    public string Name { get; set; } = "Default Tenant";
    public string? Domain { get; set; }
    public string? Description { get; set; }
    public TenantAdminSettings Admin { get; set; } = new();
}

public class TenantAdminSettings
{
    public bool Enabled { get; set; } = true;
    public string Email { get; set; } = "admin@noir.local";
    public string Password { get; set; } = "123qwe";
    public string FirstName { get; set; } = "Tenant";
    public string LastName { get; set; } = "Administrator";
}
