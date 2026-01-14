namespace NOIR.Application.Specifications.TenantSettings;

/// <summary>
/// Specification to get all settings for a specific tenant or platform defaults.
/// </summary>
public class TenantSettingsByTenantSpec : Specification<TenantSetting>
{
    public TenantSettingsByTenantSpec(string? tenantId)
    {
        if (tenantId != null)
        {
            Query.Where(s => s.TenantId == tenantId);
        }
        else
        {
            Query.Where(s => s.TenantId == null);
        }

        Query.OrderBy(s => s.Key);
        Query.TagWith("TenantSettingsByTenant");
    }
}
