namespace NOIR.Domain.Events.Order;

/// <summary>
/// Raised when a new order is created.
/// </summary>
public record OrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    string CustomerEmail,
    decimal GrandTotal,
    string Currency) : DomainEvent;

/// <summary>
/// Raised when an order status changes.
/// </summary>
public record OrderStatusChangedEvent(
    Guid OrderId,
    string OrderNumber,
    OrderStatus OldStatus,
    OrderStatus NewStatus,
    string? Reason = null) : DomainEvent;

/// <summary>
/// Raised when an order is confirmed (payment received).
/// </summary>
public record OrderConfirmedEvent(
    Guid OrderId,
    string OrderNumber) : DomainEvent;

/// <summary>
/// Raised when an order is shipped.
/// </summary>
public record OrderShippedEvent(
    Guid OrderId,
    string OrderNumber,
    string TrackingNumber,
    string ShippingCarrier) : DomainEvent;

/// <summary>
/// Raised when an order is delivered.
/// </summary>
public record OrderDeliveredEvent(
    Guid OrderId,
    string OrderNumber) : DomainEvent;

/// <summary>
/// Raised when an order is completed.
/// </summary>
public record OrderCompletedEvent(
    Guid OrderId,
    string OrderNumber) : DomainEvent;

/// <summary>
/// Raised when an order is cancelled.
/// </summary>
public record OrderCancelledEvent(
    Guid OrderId,
    string OrderNumber,
    string? CancellationReason) : DomainEvent;

/// <summary>
/// Raised when an order is refunded.
/// </summary>
public record OrderRefundedEvent(
    Guid OrderId,
    string OrderNumber,
    decimal RefundAmount) : DomainEvent;

/// <summary>
/// Raised when order notes are added.
/// </summary>
public record OrderNoteAddedEvent(
    Guid OrderId,
    string Note,
    bool IsInternal) : DomainEvent;
