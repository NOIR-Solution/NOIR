namespace NOIR.Infrastructure.Hubs;

/// <summary>
/// Strongly-typed SignalR client interface for payment-related real-time updates.
/// Avoids magic strings by defining methods that can be called on clients.
/// </summary>
public interface IPaymentClient
{
    /// <summary>
    /// Notifies the client that a payment status has changed.
    /// </summary>
    Task PaymentStatusChanged(PaymentStatusUpdate update);

    /// <summary>
    /// Notifies the client that a COD payment has been collected.
    /// </summary>
    Task CodCollected(CodCollectionUpdate update);

    /// <summary>
    /// Notifies the client that a refund status has changed.
    /// </summary>
    Task RefundStatusChanged(RefundStatusUpdate update);

    /// <summary>
    /// Notifies the client of a webhook processing result.
    /// </summary>
    Task WebhookProcessed(WebhookProcessedUpdate update);
}

/// <summary>
/// DTO for payment status change notifications.
/// </summary>
public record PaymentStatusUpdate
{
    /// <summary>
    /// The payment transaction ID.
    /// </summary>
    public required Guid TransactionId { get; init; }

    /// <summary>
    /// The NOIR transaction number.
    /// </summary>
    public required string TransactionNumber { get; init; }

    /// <summary>
    /// The previous payment status.
    /// </summary>
    public required string OldStatus { get; init; }

    /// <summary>
    /// The new payment status.
    /// </summary>
    public required string NewStatus { get; init; }

    /// <summary>
    /// When the status changed.
    /// </summary>
    public required DateTimeOffset ChangedAt { get; init; }

    /// <summary>
    /// Optional reason for the status change.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Gateway transaction ID if available.
    /// </summary>
    public string? GatewayTransactionId { get; init; }
}

/// <summary>
/// DTO for COD collection notifications.
/// </summary>
public record CodCollectionUpdate
{
    /// <summary>
    /// The payment transaction ID.
    /// </summary>
    public required Guid TransactionId { get; init; }

    /// <summary>
    /// The NOIR transaction number.
    /// </summary>
    public required string TransactionNumber { get; init; }

    /// <summary>
    /// The name of the person who collected the cash.
    /// </summary>
    public required string CollectorName { get; init; }

    /// <summary>
    /// When the cash was collected.
    /// </summary>
    public required DateTimeOffset CollectedAt { get; init; }

    /// <summary>
    /// The amount that was collected.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// The currency of the amount.
    /// </summary>
    public required string Currency { get; init; }
}

/// <summary>
/// DTO for refund status change notifications.
/// </summary>
public record RefundStatusUpdate
{
    /// <summary>
    /// The refund ID.
    /// </summary>
    public required Guid RefundId { get; init; }

    /// <summary>
    /// The NOIR refund number.
    /// </summary>
    public required string RefundNumber { get; init; }

    /// <summary>
    /// The associated payment transaction ID.
    /// </summary>
    public required Guid PaymentTransactionId { get; init; }

    /// <summary>
    /// The new refund status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// When the status changed.
    /// </summary>
    public required DateTimeOffset ChangedAt { get; init; }

    /// <summary>
    /// The refund amount.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Optional reason or notes.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// DTO for webhook processing notifications.
/// </summary>
public record WebhookProcessedUpdate
{
    /// <summary>
    /// The webhook log ID.
    /// </summary>
    public required Guid WebhookLogId { get; init; }

    /// <summary>
    /// The payment gateway provider.
    /// </summary>
    public required string Provider { get; init; }

    /// <summary>
    /// The event type from the gateway.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// The processing status.
    /// </summary>
    public required string ProcessingStatus { get; init; }

    /// <summary>
    /// When the webhook was processed.
    /// </summary>
    public required DateTimeOffset ProcessedAt { get; init; }

    /// <summary>
    /// The associated payment transaction ID if applicable.
    /// </summary>
    public Guid? PaymentTransactionId { get; init; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? Error { get; init; }
}
