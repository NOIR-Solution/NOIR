namespace NOIR.Domain.Common;

/// <summary>
/// Base class for non-aggregate entities that need audit tracking with soft delete.
/// Use this for child entities within an aggregate that need their own audit trail.
///
/// For aggregate roots, use AggregateRoot directly - it now includes IAuditableEntity.
///
/// Soft delete is handled at the REPOSITORY/INFRASTRUCTURE level:
/// - Calling repository.Remove() triggers soft delete via AuditableEntityInterceptor
/// - Global query filters automatically exclude soft-deleted entities
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditableEntity
    where TId : notnull
{
    public string? CreatedBy { get; protected set; }
    public string? ModifiedBy { get; protected set; }

    // Soft delete fields - managed by AuditableEntityInterceptor
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    protected AuditableEntity() : base()
    {
    }

    protected AuditableEntity(TId id) : base(id)
    {
    }
}
