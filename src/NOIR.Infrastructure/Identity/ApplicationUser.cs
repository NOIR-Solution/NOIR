namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Application user entity extending ASP.NET Core Identity.
/// Implements IAuditableEntity for consistent audit tracking.
/// Users are platform-level (not tenant-scoped) - tenant access is managed via UserTenantMembership.
/// </summary>
public class ApplicationUser : IdentityUser, IAuditableEntity
{
    public string? DisplayName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? RefreshToken { get; set; }
    public DateTimeOffset? RefreshTokenExpiryTime { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Path to the user's uploaded avatar in storage.
    /// When null, Gravatar or initials fallback is used.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Timestamp of the last password change.
    /// Used for security tracking and password age policies.
    /// </summary>
    public DateTimeOffset? PasswordLastChangedAt { get; set; }

    /// <summary>
    /// The tenant memberships for this user.
    /// A user can belong to multiple tenants with different roles.
    /// </summary>
    public virtual ICollection<UserTenantMembership> TenantMemberships { get; set; } = [];

    // IAuditableEntity implementation
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Soft delete (data safety - never hard delete users)
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
