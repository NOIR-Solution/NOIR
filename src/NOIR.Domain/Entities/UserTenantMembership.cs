namespace NOIR.Domain.Entities;

/// <summary>
/// Represents a user's membership and role within a specific tenant.
/// Enables multi-tenant user access where one user can belong to multiple tenants.
/// Platform-level entity - NOT scoped to any tenant (does not implement ITenantEntity).
/// </summary>
public class UserTenantMembership : Entity<Guid>, IAuditableEntity
{
    /// <summary>
    /// The user ID (FK to AspNetUsers).
    /// </summary>
    public string UserId { get; private set; } = default!;

    /// <summary>
    /// The tenant ID (FK to Tenants).
    /// </summary>
    public string TenantId { get; private set; } = default!;

    /// <summary>
    /// The user's role within this tenant.
    /// </summary>
    public TenantRole Role { get; private set; }

    /// <summary>
    /// Whether this is the user's default tenant (used when no tenant specified).
    /// Only one membership per user can be default.
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    /// When the user joined this tenant.
    /// </summary>
    public DateTimeOffset JoinedAt { get; private set; }

    #region IAuditableEntity Implementation
    // CreatedAt and ModifiedAt are inherited from Entity<Guid>

    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Navigation property to the tenant.
    /// </summary>
    public virtual Tenant Tenant { get; private set; } = default!;

    #endregion

    // Private constructor for EF Core
    private UserTenantMembership() : base() { }

    /// <summary>
    /// Creates a new user-tenant membership.
    /// </summary>
    public static UserTenantMembership Create(
        string userId,
        string tenantId,
        TenantRole role,
        bool isDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));

        return new UserTenantMembership
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Role = role,
            IsDefault = isDefault,
            JoinedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates the user's role in this tenant.
    /// </summary>
    public void UpdateRole(TenantRole newRole)
    {
        Role = newRole;
    }

    /// <summary>
    /// Sets this membership as the user's default tenant.
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
    }

    /// <summary>
    /// Removes default status from this membership.
    /// </summary>
    public void ClearDefault()
    {
        IsDefault = false;
    }

    /// <summary>
    /// Checks if the user has at least the specified role level.
    /// </summary>
    public bool HasRoleOrHigher(TenantRole minimumRole)
    {
        return (int)Role >= (int)minimumRole;
    }
}
