namespace NOIR.Domain.Common;

/// <summary>
/// Base class for aggregate roots. Aggregate roots are the entry point
/// to an aggregate and are responsible for maintaining consistency.
/// All aggregate roots are auditable with soft delete support by default.
/// Inherits CreatedAt and ModifiedAt from Entity.
/// </summary>
/// <typeparam name="TId">The type of the aggregate root's identifier.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot, IAuditableEntity
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    #region IAuditableEntity Implementation (CreatedAt/ModifiedAt inherited from Entity)

    public string? CreatedBy { get; protected set; }
    public string? ModifiedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public DateTimeOffset? DeletedAt { get; protected set; }
    public string? DeletedBy { get; protected set; }

    #endregion

    protected AggregateRoot() : base()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {
    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Marker interface for aggregate roots.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
