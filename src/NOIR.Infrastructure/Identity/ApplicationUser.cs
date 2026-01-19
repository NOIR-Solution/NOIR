namespace NOIR.Infrastructure.Identity;

/// <summary>
/// Application user entity extending ASP.NET Core Identity.
/// Implements IAuditableEntity for consistent audit tracking.
/// Each user belongs to exactly one tenant (single-tenant-per-user model).
/// Email is unique within a tenant, not globally.
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
    /// The tenant this user belongs to.
    /// Each user belongs to exactly one tenant.
    /// Email uniqueness is scoped to tenant (same email can exist in different tenants).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// When the user was locked (IsActive set to false).
    /// </summary>
    public DateTimeOffset? LockedAt { get; set; }

    /// <summary>
    /// User ID who locked this user.
    /// </summary>
    public string? LockedBy { get; set; }

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
    /// Indicates if this is a protected system user (e.g., admin@noir.local).
    /// System users cannot be deleted, locked, or have all their roles removed.
    /// </summary>
    public bool IsSystemUser { get; set; }

    // IAuditableEntity implementation
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }

    // Soft delete (data safety - never hard delete users)
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public string FullName => !string.IsNullOrWhiteSpace(DisplayName)
        ? DisplayName
        : $"{FirstName} {LastName}".Trim();
}
