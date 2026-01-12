namespace NOIR.Application.Features.Audit.Specifications;

/// <summary>
/// Specification to find an audit retention policy by its ID with tracking enabled for mutations.
/// </summary>
public sealed class AuditRetentionPolicyByIdTrackingSpec : Specification<AuditRetentionPolicy>
{
    public AuditRetentionPolicyByIdTrackingSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .AsTracking()
             .TagWith("AuditRetentionPolicyByIdTracking");
    }
}
