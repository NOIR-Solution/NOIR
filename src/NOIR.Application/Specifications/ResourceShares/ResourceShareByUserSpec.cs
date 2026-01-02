namespace NOIR.Application.Specifications.ResourceShares;

/// <summary>
/// Specification to find a specific resource share for a user.
/// Used for authorization checks.
/// </summary>
public class ResourceShareByUserSpec : Specification<ResourceShare>
{
    public ResourceShareByUserSpec(string resourceType, Guid resourceId, string userId)
    {
        Query.Where(s => s.ResourceType == resourceType.ToLowerInvariant())
             .Where(s => s.ResourceId == resourceId)
             .Where(s => s.SharedWithUserId == userId)
             .Where(s => s.ExpiresAt == null || s.ExpiresAt > DateTimeOffset.UtcNow)
             .TagWith("ResourceShareByUser");
    }
}
