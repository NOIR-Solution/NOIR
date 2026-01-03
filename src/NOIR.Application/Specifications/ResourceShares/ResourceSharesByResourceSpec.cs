namespace NOIR.Application.Specifications.ResourceShares;

/// <summary>
/// Specification to find all shares for a specific resource.
/// Used for listing who has access to a resource.
/// </summary>
public sealed class ResourceSharesByResourceSpec : Specification<ResourceShare>
{
    public ResourceSharesByResourceSpec(string resourceType, Guid resourceId)
    {
        Query.Where(s => s.ResourceType == resourceType.ToLowerInvariant())
             .Where(s => s.ResourceId == resourceId)
             .Where(s => s.ExpiresAt == null || s.ExpiresAt > DateTimeOffset.UtcNow)
             .TagWith("ResourceSharesByResource");
    }
}
