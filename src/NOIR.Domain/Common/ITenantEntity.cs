namespace NOIR.Domain.Common;

/// <summary>
/// Interface for entities that belong to a specific tenant.
/// Used for multi-tenancy support with Finbuckle.MultiTenant.
/// TenantId should be immutable after entity creation to prevent
/// accidental or malicious cross-tenant data access.
/// </summary>
public interface ITenantEntity
{
    /// <summary>
    /// The tenant ID that this entity belongs to.
    /// Set automatically by TenantIdSetterInterceptor from current tenant context.
    /// Uses protected setter to enforce immutability after creation.
    /// EF Core can still set this value via backing field.
    /// </summary>
    string? TenantId { get; }
}

/// <summary>
/// Base class for tenant-specific entities with audit tracking and soft delete.
/// Implements IAuditableEntity so that AuditableEntityInterceptor automatically
/// converts Remove() calls to soft-delete for all tenant entities.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class TenantEntity<TId> : Entity<TId>, ITenantEntity, IAuditableEntity
    where TId : notnull
{
    public string? TenantId { get; protected set; }

    // Audit fields - managed by AuditableEntityInterceptor
    public string? CreatedBy { get; protected set; }
    public string? ModifiedBy { get; protected set; }

    // Soft delete fields - managed by AuditableEntityInterceptor
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    protected TenantEntity() : base()
    {
    }

    protected TenantEntity(TId id, string? tenantId = null) : base(id)
    {
        TenantId = tenantId;
    }
}

/// <summary>
/// Base class for tenant-specific aggregate roots.
/// Includes soft delete and audit tracking since AggregateRoot implements IAuditableEntity.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
public abstract class TenantAggregateRoot<TId> : AggregateRoot<TId>, ITenantEntity
    where TId : notnull
{
    public string? TenantId { get; protected set; }

    protected TenantAggregateRoot() : base()
    {
    }

    protected TenantAggregateRoot(TId id, string? tenantId = null) : base(id)
    {
        TenantId = tenantId;
    }
}
