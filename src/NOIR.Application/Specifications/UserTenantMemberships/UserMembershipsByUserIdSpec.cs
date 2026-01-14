namespace NOIR.Application.Specifications.UserTenantMemberships;

/// <summary>
/// Specification to get all tenant memberships for a user.
/// </summary>
public class UserMembershipsByUserIdSpec : Specification<UserTenantMembership>
{
    public UserMembershipsByUserIdSpec(string userId, string? excludeTenantId = null)
    {
        Query.Where(m => m.UserId == userId)
             .Include(m => m.Tenant)
             .OrderByDescending(m => m.IsDefault)
             .ThenBy(m => m.JoinedAt);

        if (excludeTenantId != null)
        {
            Query.Where(m => m.TenantId != excludeTenantId);
        }

        Query.TagWith("UserMembershipsByUserId");
    }
}
