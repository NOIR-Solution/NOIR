namespace NOIR.Application.Specifications.UserTenantMemberships;

/// <summary>
/// Specification to get all user memberships in a tenant.
/// </summary>
public class TenantMembershipsByTenantIdSpec : Specification<UserTenantMembership>
{
    public TenantMembershipsByTenantIdSpec(string tenantId)
    {
        Query.Where(m => m.TenantId == tenantId)
             .OrderByDescending(m => m.Role)
             .ThenBy(m => m.JoinedAt);

        Query.TagWith("TenantMembershipsByTenantId");
    }
}
