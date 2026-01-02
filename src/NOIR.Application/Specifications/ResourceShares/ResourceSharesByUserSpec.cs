namespace NOIR.Application.Specifications.ResourceShares;

/// <summary>
/// Specification to find all resource shares for a user.
/// Optionally filter by resource type.
/// Used for listing accessible resources.
/// </summary>
public class ResourceSharesByUserSpec : Specification<ResourceShare>
{
    public ResourceSharesByUserSpec(string userId, string? resourceType = null)
    {
        Query.Where(s => s.SharedWithUserId == userId)
             .Where(s => s.ExpiresAt == null || s.ExpiresAt > DateTimeOffset.UtcNow)
             .TagWith("ResourceSharesByUser");

        if (!string.IsNullOrEmpty(resourceType))
        {
            Query.Where(s => s.ResourceType == resourceType.ToLowerInvariant());
        }
    }
}
