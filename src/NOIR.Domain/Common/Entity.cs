namespace NOIR.Domain.Common;

/// <summary>
/// Base class for all domain entities with a strongly-typed identifier.
/// Uses DateTimeOffset for timezone-aware timestamps.
/// </summary>
/// <typeparam name="TId">The type of the entity's identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    public TId Id { get; protected set; } = default!;

    /// <summary>
    /// Timestamp when the entity was created.
    /// For IAuditableEntity implementations, this is set automatically by AuditableEntityInterceptor.
    /// For plain Entity instances, this defaults to UtcNow on construction.
    /// </summary>
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Timestamp when the entity was last modified.
    /// For IAuditableEntity implementations, this is set automatically by AuditableEntityInterceptor.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; protected set; }

    protected Entity()
    {
    }

    protected Entity(TId id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> entity && Equals(entity);
    }

    public bool Equals(Entity<TId>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return EqualityComparer<TId>.Default.GetHashCode(Id);
    }

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
