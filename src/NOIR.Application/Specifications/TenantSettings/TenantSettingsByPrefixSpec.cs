namespace NOIR.Application.Specifications.TenantSettings;

/// <summary>
/// Specification to find tenant settings by key prefix.
/// </summary>
public class TenantSettingsByPrefixSpec : Specification<TenantSetting>
{
    public TenantSettingsByPrefixSpec(string? tenantId, string keyPrefix)
    {
        if (tenantId != null)
        {
            Query.Where(s => s.TenantId == tenantId && s.Key.StartsWith(keyPrefix));
        }
        else
        {
            Query.Where(s => s.TenantId == null && s.Key.StartsWith(keyPrefix));
        }

        Query.OrderBy(s => s.Key);
        Query.TagWith("TenantSettingsByPrefix");
    }
}
