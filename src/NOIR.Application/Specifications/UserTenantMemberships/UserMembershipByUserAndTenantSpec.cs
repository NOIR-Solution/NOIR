namespace NOIR.Application.Specifications.UserTenantMemberships;

/// <summary>
/// Specification to find a specific user's membership in a tenant.
/// </summary>
public class UserMembershipByUserAndTenantSpec : Specification<UserTenantMembership>
{
    public UserMembershipByUserAndTenantSpec(string userId, string tenantId, bool asTracking = false)
    {
        Query.Where(m => m.UserId == userId && m.TenantId == tenantId)
             .Include(m => m.Tenant);

        if (asTracking)
        {
            Query.AsTracking();
        }

        Query.TagWith("UserMembershipByUserAndTenant");
    }
}
