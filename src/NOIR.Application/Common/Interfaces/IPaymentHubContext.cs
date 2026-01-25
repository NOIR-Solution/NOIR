namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Abstraction for SignalR hub context to enable real-time payment update delivery.
/// Implemented in Infrastructure layer using IHubContext&lt;PaymentHub, IPaymentClient&gt;.
/// </summary>
public interface IPaymentHubContext
{
    /// <summary>
    /// Sends a payment status update to clients tracking a specific transaction.
    /// </summary>
    Task SendPaymentStatusUpdateAsync(
        Guid transactionId,
        string transactionNumber,
        string oldStatus,
        string newStatus,
        string? reason = null,
        string? gatewayTransactionId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a COD collection update to admin clients.
    /// </summary>
    Task SendCodCollectionUpdateAsync(
        string tenantId,
        Guid transactionId,
        string transactionNumber,
        string collectorName,
        decimal amount,
        string currency,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a refund status update to interested clients.
    /// </summary>
    Task SendRefundStatusUpdateAsync(
        Guid refundId,
        string refundNumber,
        Guid paymentTransactionId,
        string status,
        decimal amount,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a webhook processing update to admin clients monitoring webhooks.
    /// </summary>
    Task SendWebhookProcessedUpdateAsync(
        string tenantId,
        Guid webhookLogId,
        string provider,
        string eventType,
        string processingStatus,
        Guid? paymentTransactionId = null,
        string? error = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends a payment status update to all clients tracking an order's payments.
    /// </summary>
    Task SendOrderPaymentUpdateAsync(
        Guid orderId,
        Guid transactionId,
        string transactionNumber,
        string status,
        CancellationToken ct = default);
}
