namespace NOIR.Application.Features.FeatureManagement.Specifications;

/// <summary>
/// Specification to get all module states for a tenant.
/// </summary>
public sealed class TenantModuleStateByTenantSpec : Specification<TenantModuleState>
{
    public TenantModuleStateByTenantSpec(string tenantId)
    {
        Query.Where(x => x.TenantId == tenantId)
             .TagWith("TenantModuleStateByTenant");
    }
}

/// <summary>
/// Specification to get a specific module state for a tenant (with tracking for mutations).
/// </summary>
public sealed class TenantModuleStateByFeatureSpec : Specification<TenantModuleState>
{
    public TenantModuleStateByFeatureSpec(string tenantId, string featureName)
    {
        Query.Where(x => x.TenantId == tenantId && x.FeatureName == featureName)
             .AsTracking()
             .TagWith("TenantModuleStateByFeature");
    }
}
