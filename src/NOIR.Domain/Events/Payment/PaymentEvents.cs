namespace NOIR.Domain.Events.Payment;

/// <summary>
/// Raised when a new payment transaction is created.
/// </summary>
public record PaymentCreatedEvent(
    Guid TransactionId,
    string TransactionNumber,
    decimal Amount,
    string Currency,
    string Provider) : DomainEvent;

/// <summary>
/// Raised when a payment transaction status changes.
/// </summary>
public record PaymentStatusChangedEvent(
    Guid TransactionId,
    PaymentStatus OldStatus,
    PaymentStatus NewStatus,
    string? Reason = null) : DomainEvent;

/// <summary>
/// Raised when a payment is successfully completed.
/// </summary>
public record PaymentSucceededEvent(
    Guid TransactionId,
    string Provider,
    decimal Amount,
    string? GatewayTransactionId) : DomainEvent;

/// <summary>
/// Raised when a payment fails.
/// </summary>
public record PaymentFailedEvent(
    Guid TransactionId,
    string Reason,
    string? FailureCode) : DomainEvent;

/// <summary>
/// Raised when a COD payment is collected by courier.
/// </summary>
public record CodCollectedEvent(
    Guid TransactionId,
    string CollectorName,
    DateTimeOffset CollectedAt) : DomainEvent;

/// <summary>
/// Raised when a new payment gateway is configured.
/// </summary>
public record PaymentGatewayCreatedEvent(
    Guid GatewayId,
    string Provider) : DomainEvent;

/// <summary>
/// Raised when a refund is requested.
/// </summary>
public record RefundRequestedEvent(
    Guid RefundId,
    Guid TransactionId,
    decimal Amount,
    RefundReason Reason) : DomainEvent;

/// <summary>
/// Raised when a refund is completed.
/// </summary>
public record RefundCompletedEvent(
    Guid RefundId,
    Guid TransactionId,
    decimal Amount) : DomainEvent;

// ==================== Subscription Events ====================

/// <summary>
/// Raised when a new subscription plan is created.
/// </summary>
public record SubscriptionPlanCreatedEvent(
    Guid PlanId,
    string Name,
    decimal Price,
    BillingInterval Interval) : DomainEvent;

/// <summary>
/// Raised when a new subscription is created.
/// </summary>
public record SubscriptionCreatedEvent(
    Guid SubscriptionId,
    Guid CustomerId,
    Guid PlanId,
    decimal Amount,
    SubscriptionStatus Status) : DomainEvent;

/// <summary>
/// Raised when a subscription status changes.
/// </summary>
public record SubscriptionStatusChangedEvent(
    Guid SubscriptionId,
    SubscriptionStatus OldStatus,
    SubscriptionStatus NewStatus) : DomainEvent;

/// <summary>
/// Raised when a subscription is cancelled.
/// </summary>
public record SubscriptionCancelledEvent(
    Guid SubscriptionId,
    Guid CustomerId,
    bool AtPeriodEnd) : DomainEvent;

/// <summary>
/// Raised when a subscription is renewed.
/// </summary>
public record SubscriptionRenewedEvent(
    Guid SubscriptionId,
    Guid CustomerId,
    decimal Amount,
    DateTimeOffset NewPeriodEnd) : DomainEvent;
