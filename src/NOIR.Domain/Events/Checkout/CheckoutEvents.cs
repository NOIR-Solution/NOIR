namespace NOIR.Domain.Events.Checkout;

/// <summary>
/// Raised when a checkout session is created.
/// </summary>
public record CheckoutSessionCreatedEvent(
    Guid SessionId,
    Guid CartId,
    string? UserId) : DomainEvent;

/// <summary>
/// Raised when checkout session status changes.
/// </summary>
public record CheckoutSessionStatusChangedEvent(
    Guid SessionId,
    CheckoutSessionStatus OldStatus,
    CheckoutSessionStatus NewStatus) : DomainEvent;

/// <summary>
/// Raised when shipping address is set during checkout.
/// </summary>
public record CheckoutAddressSetEvent(
    Guid SessionId,
    string AddressType) : DomainEvent;

/// <summary>
/// Raised when shipping method is selected during checkout.
/// </summary>
public record CheckoutShippingSelectedEvent(
    Guid SessionId,
    string ShippingMethod,
    decimal ShippingCost) : DomainEvent;

/// <summary>
/// Raised when checkout is completed and order is created.
/// </summary>
public record CheckoutCompletedEvent(
    Guid SessionId,
    Guid OrderId,
    string OrderNumber,
    decimal GrandTotal) : DomainEvent;

/// <summary>
/// Raised when checkout session expires.
/// </summary>
public record CheckoutExpiredEvent(
    Guid SessionId,
    Guid CartId) : DomainEvent;

/// <summary>
/// Raised when checkout is abandoned by user.
/// </summary>
public record CheckoutAbandonedEvent(
    Guid SessionId,
    Guid CartId,
    CheckoutSessionStatus LastStatus) : DomainEvent;
