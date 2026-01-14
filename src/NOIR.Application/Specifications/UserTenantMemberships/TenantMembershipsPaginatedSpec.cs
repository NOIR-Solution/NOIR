namespace NOIR.Application.Specifications.UserTenantMemberships;

/// <summary>
/// Specification to get paginated user memberships in a tenant.
/// </summary>
public class TenantMembershipsPaginatedSpec : Specification<UserTenantMembership>
{
    public TenantMembershipsPaginatedSpec(string tenantId, int pageNumber, int pageSize)
    {
        Query.Where(m => m.TenantId == tenantId)
             .OrderByDescending(m => m.Role)
             .ThenBy(m => m.JoinedAt)
             .Skip((pageNumber - 1) * pageSize)
             .Take(pageSize);

        Query.TagWith("TenantMembershipsPaginated");
    }
}
