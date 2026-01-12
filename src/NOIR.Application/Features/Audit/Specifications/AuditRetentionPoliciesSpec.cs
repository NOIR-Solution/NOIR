namespace NOIR.Application.Features.Audit.Specifications;

/// <summary>
/// Specification to retrieve audit retention policies with optional filtering.
/// </summary>
public sealed class AuditRetentionPoliciesSpec : Specification<AuditRetentionPolicy>
{
    public AuditRetentionPoliciesSpec(string? tenantId = null, bool includeInactive = false)
    {
        Query.Where(p => tenantId == null || p.TenantId == tenantId)
             .Where(p => includeInactive || p.IsActive)
             .OrderBy(p => p.Priority)
             .ThenBy(p => p.Name)
             .TagWith("AuditRetentionPolicies");
    }
}
