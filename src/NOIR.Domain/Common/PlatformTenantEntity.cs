namespace NOIR.Domain.Common;

/// <summary>
/// Base class for entities that support platform defaults with tenant overrides.
/// Platform entities (TenantId = null) serve as defaults for all tenants.
/// Tenant entities (TenantId = value) are tenant-specific overrides.
///
/// Includes full audit capabilities (IAuditableEntity):
/// - CreatedAt, ModifiedAt (timestamps from Entity)
/// - CreatedBy, ModifiedBy, DeletedBy (user tracking)
/// - IsDeleted, DeletedAt (soft delete support)
///
/// For entities that also need domain events, use PlatformTenantAggregateRoot instead.
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class PlatformTenantEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : notnull
{
    /// <summary>
    /// The tenant this entity belongs to.
    /// NULL = platform default (shared across all tenants).
    /// Non-null = tenant-specific override.
    /// </summary>
    public string? TenantId { get; protected set; }

    /// <summary>
    /// Whether this is a platform-level default entity.
    /// Platform entities are shared across all tenants.
    /// </summary>
    public bool IsPlatformDefault => TenantId == null;

    /// <summary>
    /// Whether this is a tenant-specific override.
    /// Tenant overrides are created via copy-on-edit when a tenant customizes a platform entity.
    /// </summary>
    public bool IsTenantOverride => TenantId != null;

    #region IAuditableEntity Implementation

    public string? CreatedBy { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    #endregion

    protected PlatformTenantEntity() : base() { }

    protected PlatformTenantEntity(TId id) : base(id) { }
}
