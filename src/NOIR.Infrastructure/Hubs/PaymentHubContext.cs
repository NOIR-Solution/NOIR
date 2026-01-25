namespace NOIR.Infrastructure.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// Implementation of IPaymentHubContext using SignalR.
/// Provides abstraction for sending real-time payment updates.
/// </summary>
public class PaymentHubContext : IPaymentHubContext, IScopedService
{
    private readonly IHubContext<PaymentHub, IPaymentClient> _hubContext;
    private readonly ILogger<PaymentHubContext> _logger;

    public PaymentHubContext(
        IHubContext<PaymentHub, IPaymentClient> hubContext,
        ILogger<PaymentHubContext> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendPaymentStatusUpdateAsync(
        Guid transactionId,
        string transactionNumber,
        string oldStatus,
        string newStatus,
        string? reason = null,
        string? gatewayTransactionId = null,
        CancellationToken ct = default)
    {
        try
        {
            var update = new PaymentStatusUpdate
            {
                TransactionId = transactionId,
                TransactionNumber = transactionNumber,
                OldStatus = oldStatus,
                NewStatus = newStatus,
                ChangedAt = DateTimeOffset.UtcNow,
                Reason = reason,
                GatewayTransactionId = gatewayTransactionId
            };

            // Send to clients tracking this specific payment
            await _hubContext.Clients.Group($"payment_{transactionId}")
                .PaymentStatusChanged(update);

            _logger.LogDebug(
                "Sent payment status update for transaction {TransactionId}: {OldStatus} -> {NewStatus}",
                transactionId, oldStatus, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send payment status update for transaction {TransactionId}",
                transactionId);
        }
    }

    /// <inheritdoc />
    public async Task SendCodCollectionUpdateAsync(
        string tenantId,
        Guid transactionId,
        string transactionNumber,
        string collectorName,
        decimal amount,
        string currency,
        CancellationToken ct = default)
    {
        try
        {
            var update = new CodCollectionUpdate
            {
                TransactionId = transactionId,
                TransactionNumber = transactionNumber,
                CollectorName = collectorName,
                CollectedAt = DateTimeOffset.UtcNow,
                Amount = amount,
                Currency = currency
            };

            // Send to admin clients tracking COD updates for this tenant
            await _hubContext.Clients.Group($"cod_updates_{tenantId}")
                .CodCollected(update);

            // Also send to clients tracking this specific payment
            await _hubContext.Clients.Group($"payment_{transactionId}")
                .CodCollected(update);

            _logger.LogDebug(
                "Sent COD collection update for transaction {TransactionId} by {CollectorName}",
                transactionId, collectorName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send COD collection update for transaction {TransactionId}",
                transactionId);
        }
    }

    /// <inheritdoc />
    public async Task SendRefundStatusUpdateAsync(
        Guid refundId,
        string refundNumber,
        Guid paymentTransactionId,
        string status,
        decimal amount,
        string? reason = null,
        CancellationToken ct = default)
    {
        try
        {
            var update = new RefundStatusUpdate
            {
                RefundId = refundId,
                RefundNumber = refundNumber,
                PaymentTransactionId = paymentTransactionId,
                Status = status,
                ChangedAt = DateTimeOffset.UtcNow,
                Amount = amount,
                Reason = reason
            };

            // Send to clients tracking the original payment
            await _hubContext.Clients.Group($"payment_{paymentTransactionId}")
                .RefundStatusChanged(update);

            _logger.LogDebug(
                "Sent refund status update for refund {RefundId}: {Status}",
                refundId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send refund status update for refund {RefundId}",
                refundId);
        }
    }

    /// <inheritdoc />
    public async Task SendWebhookProcessedUpdateAsync(
        string tenantId,
        Guid webhookLogId,
        string provider,
        string eventType,
        string processingStatus,
        Guid? paymentTransactionId = null,
        string? error = null,
        CancellationToken ct = default)
    {
        try
        {
            var update = new WebhookProcessedUpdate
            {
                WebhookLogId = webhookLogId,
                Provider = provider,
                EventType = eventType,
                ProcessingStatus = processingStatus,
                ProcessedAt = DateTimeOffset.UtcNow,
                PaymentTransactionId = paymentTransactionId,
                Error = error
            };

            // Send to admin clients monitoring webhooks for this tenant
            await _hubContext.Clients.Group($"webhooks_{tenantId}")
                .WebhookProcessed(update);

            _logger.LogDebug(
                "Sent webhook processed update for log {WebhookLogId}: {Provider}/{EventType} -> {Status}",
                webhookLogId, provider, eventType, processingStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send webhook processed update for log {WebhookLogId}",
                webhookLogId);
        }
    }

    /// <inheritdoc />
    public async Task SendOrderPaymentUpdateAsync(
        Guid orderId,
        Guid transactionId,
        string transactionNumber,
        string status,
        CancellationToken ct = default)
    {
        try
        {
            var update = new PaymentStatusUpdate
            {
                TransactionId = transactionId,
                TransactionNumber = transactionNumber,
                OldStatus = string.Empty,
                NewStatus = status,
                ChangedAt = DateTimeOffset.UtcNow
            };

            // Send to clients tracking payments for this order
            await _hubContext.Clients.Group($"order_{orderId}")
                .PaymentStatusChanged(update);

            _logger.LogDebug(
                "Sent order payment update for order {OrderId}, transaction {TransactionId}: {Status}",
                orderId, transactionId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send order payment update for order {OrderId}",
                orderId);
        }
    }
}
