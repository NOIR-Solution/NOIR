namespace NOIR.Domain.Events.Shipping;

/// <summary>
/// Domain event raised when a shipping provider is created.
/// </summary>
public sealed record ShippingProviderCreatedEvent(
    Guid ProviderId,
    ShippingProviderCode ProviderCode) : DomainEvent;

/// <summary>
/// Domain event raised when a shipping provider is configured with credentials.
/// </summary>
public sealed record ShippingProviderConfiguredEvent(
    Guid ProviderId,
    ShippingProviderCode ProviderCode,
    GatewayEnvironment Environment) : DomainEvent;

/// <summary>
/// Domain event raised when a shipping provider is activated.
/// </summary>
public sealed record ShippingProviderActivatedEvent(
    Guid ProviderId,
    ShippingProviderCode ProviderCode) : DomainEvent;

/// <summary>
/// Domain event raised when a shipping provider is deactivated.
/// </summary>
public sealed record ShippingProviderDeactivatedEvent(
    Guid ProviderId,
    ShippingProviderCode ProviderCode) : DomainEvent;

// ========== Shipping Order Events ==========

/// <summary>
/// Domain event raised when a shipping order is created (draft).
/// </summary>
public sealed record ShippingOrderCreatedEvent(
    Guid ShippingOrderId,
    Guid OrderId,
    ShippingProviderCode ProviderCode) : DomainEvent;

/// <summary>
/// Domain event raised when a shipping order is submitted to provider.
/// </summary>
public sealed record ShippingOrderSubmittedEvent(
    Guid ShippingOrderId,
    string TrackingNumber,
    ShippingProviderCode ProviderCode) : DomainEvent;

/// <summary>
/// Domain event raised when shipping order status changes.
/// </summary>
public sealed record ShippingOrderStatusChangedEvent(
    Guid ShippingOrderId,
    string TrackingNumber,
    ShippingStatus PreviousStatus,
    ShippingStatus NewStatus,
    string? Location) : DomainEvent;

/// <summary>
/// Domain event raised when a shipping order is cancelled.
/// </summary>
public sealed record ShippingOrderCancelledEvent(
    Guid ShippingOrderId,
    string TrackingNumber,
    ShippingStatus PreviousStatus,
    string? Reason) : DomainEvent;

/// <summary>
/// Domain event raised when a shipping order is delivered.
/// </summary>
public sealed record ShippingOrderDeliveredEvent(
    Guid ShippingOrderId,
    Guid OrderId,
    string TrackingNumber,
    DateTimeOffset DeliveredAt) : DomainEvent;
