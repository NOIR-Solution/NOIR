namespace NOIR.Domain.Common;

/// <summary>
/// Base aggregate root for entities that support platform defaults with tenant overrides
/// and also need domain event capabilities.
/// Platform entities (TenantId = null) serve as defaults for all tenants.
/// Tenant entities (TenantId = value) are tenant-specific overrides.
///
/// Inherits full audit capabilities from AggregateRoot:
/// - CreatedAt, ModifiedAt (timestamps from Entity)
/// - CreatedBy, ModifiedBy, DeletedBy (user tracking)
/// - IsDeleted, DeletedAt (soft delete support)
/// - Domain event management (AddDomainEvent, ClearDomainEvents)
/// </summary>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class PlatformTenantAggregateRoot<TId> : AggregateRoot<TId>
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

    protected PlatformTenantAggregateRoot() : base() { }

    protected PlatformTenantAggregateRoot(TId id) : base(id) { }
}
