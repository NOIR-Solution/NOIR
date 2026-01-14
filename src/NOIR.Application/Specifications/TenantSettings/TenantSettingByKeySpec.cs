namespace NOIR.Application.Specifications.TenantSettings;

/// <summary>
/// Specification to find a tenant setting by tenant ID and key.
/// </summary>
public class TenantSettingByKeySpec : Specification<TenantSetting>
{
    public TenantSettingByKeySpec(string? tenantId, string key, bool asTracking = false)
    {
        if (tenantId != null)
        {
            Query.Where(s => s.TenantId == tenantId && s.Key == key);
        }
        else
        {
            Query.Where(s => s.TenantId == null && s.Key == key);
        }

        if (asTracking)
        {
            Query.AsTracking();
        }

        Query.TagWith("TenantSettingByKey");
    }
}
