namespace NOIR.Application.Features.Audit.Specifications;

/// <summary>
/// Specification to find an audit retention policy by its ID (read-only).
/// </summary>
public sealed class AuditRetentionPolicyByIdSpec : Specification<AuditRetentionPolicy>
{
    public AuditRetentionPolicyByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .TagWith("AuditRetentionPolicyById");
    }
}
