namespace NOIR.Domain.Entities.Shipping;

/// <summary>
/// A tracking event for a shipping order.
/// Pushed via webhooks or pulled via polling from providers.
/// </summary>
public class ShippingTrackingEvent : TenantEntity<Guid>
{
    private ShippingTrackingEvent() : base() { }
    private ShippingTrackingEvent(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// The shipping order this event belongs to.
    /// </summary>
    public Guid ShippingOrderId { get; private set; }

    /// <summary>
    /// Provider's event type code.
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Mapped shipping status.
    /// </summary>
    public ShippingStatus Status { get; private set; }

    /// <summary>
    /// Human-readable description of the event.
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Location where the event occurred.
    /// </summary>
    public string? Location { get; private set; }

    /// <summary>
    /// When the event occurred (from provider).
    /// </summary>
    public DateTimeOffset EventDate { get; private set; }

    /// <summary>
    /// When we received this event.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>
    /// Raw JSON payload from the provider (for debugging).
    /// </summary>
    public string? RawPayload { get; private set; }

    /// <summary>
    /// Navigation property to shipping order.
    /// </summary>
    public ShippingOrder? ShippingOrder { get; private set; }

    public static ShippingTrackingEvent Create(
        Guid shippingOrderId,
        string eventType,
        ShippingStatus status,
        string description,
        string? location,
        DateTimeOffset eventDate,
        string? rawPayload = null,
        string? tenantId = null)
    {
        return new ShippingTrackingEvent(Guid.NewGuid(), tenantId)
        {
            ShippingOrderId = shippingOrderId,
            EventType = eventType,
            Status = status,
            Description = description,
            Location = location,
            EventDate = eventDate,
            ReceivedAt = DateTimeOffset.UtcNow,
            RawPayload = rawPayload
        };
    }
}
