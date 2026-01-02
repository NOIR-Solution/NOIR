namespace NOIR.Domain.Common;

/// <summary>
/// Marker interface for domain events.
/// Domain events represent something that happened in the domain.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Base record for domain events with common properties.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
