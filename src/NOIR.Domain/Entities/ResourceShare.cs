namespace NOIR.Domain.Entities;

/// <summary>
/// Represents a share of a resource with a specific user.
/// Enables fine-grained access control beyond role-based permissions.
/// </summary>
public class ResourceShare : TenantEntity<Guid>, IAuditableEntity
{
    /// <summary>
    /// Type of the shared resource (e.g., "document", "folder").
    /// </summary>
    public string ResourceType { get; private set; } = default!;

    /// <summary>
    /// ID of the shared resource.
    /// </summary>
    public Guid ResourceId { get; private set; }

    /// <summary>
    /// User ID the resource is shared with.
    /// </summary>
    public string SharedWithUserId { get; private set; } = default!;

    /// <summary>
    /// Permission level granted to the user.
    /// </summary>
    public SharePermission Permission { get; private set; }

    /// <summary>
    /// Optional expiration date for the share.
    /// Null means no expiration.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// User who created the share (who shared the resource).
    /// </summary>
    public string? SharedByUserId { get; private set; }

    #region IAuditableEntity Implementation

    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    #endregion

    // Private constructor for EF Core
    private ResourceShare() : base() { }

    /// <summary>
    /// Creates a new resource share.
    /// </summary>
    public static ResourceShare Create(
        string resourceType,
        Guid resourceId,
        string sharedWithUserId,
        SharePermission permission,
        string? sharedByUserId = null,
        DateTimeOffset? expiresAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(sharedWithUserId);

        if (resourceId == Guid.Empty)
            throw new ArgumentException("ResourceId cannot be empty.", nameof(resourceId));

        return new ResourceShare
        {
            Id = Guid.NewGuid(),
            ResourceType = resourceType.ToLowerInvariant(),
            ResourceId = resourceId,
            SharedWithUserId = sharedWithUserId,
            Permission = permission,
            SharedByUserId = sharedByUserId,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Updates the permission level.
    /// </summary>
    public void UpdatePermission(SharePermission newPermission)
    {
        Permission = newPermission;
    }

    /// <summary>
    /// Updates the expiration date.
    /// </summary>
    public void UpdateExpiration(DateTimeOffset? expiresAt)
    {
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Checks if the share is currently valid (not expired).
    /// </summary>
    public bool IsValid() => ExpiresAt == null || ExpiresAt > DateTimeOffset.UtcNow;

    /// <summary>
    /// Checks if this share grants the specified action.
    /// </summary>
    public bool AllowsAction(string action) => IsValid() && Permission.Allows(action);
}
