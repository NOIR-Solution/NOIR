namespace NOIR.Application.Features.Payments.EventHandlers;

/// <summary>
/// Handles payment domain events by sending real-time SignalR updates and in-app notifications.
/// </summary>
public class PaymentNotificationHandler
{
    private readonly IPaymentHubContext _paymentHub;
    private readonly INotificationService _notificationService;
    private readonly IRepository<PaymentTransaction, Guid> _transactionRepository;
    private readonly ILogger<PaymentNotificationHandler> _logger;

    public PaymentNotificationHandler(
        IPaymentHubContext paymentHub,
        INotificationService notificationService,
        IRepository<PaymentTransaction, Guid> transactionRepository,
        ILogger<PaymentNotificationHandler> logger)
    {
        _paymentHub = paymentHub;
        _notificationService = notificationService;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task Handle(PaymentSucceededEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Processing payment success notification for transaction {TransactionId}", evt.TransactionId);

        var transaction = await _transactionRepository.GetByIdAsync(evt.TransactionId, ct);
        if (transaction is null)
        {
            _logger.LogWarning("PaymentTransaction {TransactionId} not found for success notification", evt.TransactionId);
            return;
        }

        try
        {
            await _paymentHub.SendPaymentStatusUpdateAsync(
                evt.TransactionId,
                transaction.TransactionNumber,
                PaymentStatus.Pending.ToString(),
                PaymentStatus.Paid.ToString(),
                gatewayTransactionId: evt.GatewayTransactionId,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR payment success update for {TransactionId}", evt.TransactionId);
        }

        try
        {
            if (transaction.OrderId.HasValue)
            {
                await _paymentHub.SendOrderPaymentUpdateAsync(
                    transaction.OrderId.Value,
                    evt.TransactionId,
                    transaction.TransactionNumber,
                    PaymentStatus.Paid.ToString(),
                    ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR order payment update for {TransactionId}", evt.TransactionId);
        }

        try
        {
            await _notificationService.SendToRoleAsync(
                "admin",
                NotificationType.Success,
                NotificationCategory.Workflow,
                "Payment Received",
                $"Payment {transaction.TransactionNumber} of {evt.Amount} completed via {evt.Provider}.",
                actionUrl: transaction.OrderId.HasValue ? $"/orders/{transaction.OrderId}" : null,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send in-app notification for payment {TransactionId}", evt.TransactionId);
        }
    }

    public async Task Handle(PaymentFailedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Processing payment failure notification for transaction {TransactionId}", evt.TransactionId);

        var transaction = await _transactionRepository.GetByIdAsync(evt.TransactionId, ct);
        if (transaction is null)
        {
            _logger.LogWarning("PaymentTransaction {TransactionId} not found for failure notification", evt.TransactionId);
            return;
        }

        try
        {
            await _paymentHub.SendPaymentStatusUpdateAsync(
                evt.TransactionId,
                transaction.TransactionNumber,
                PaymentStatus.Pending.ToString(),
                PaymentStatus.Failed.ToString(),
                reason: evt.Reason,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR payment failure update for {TransactionId}", evt.TransactionId);
        }

        try
        {
            await _notificationService.SendToRoleAsync(
                "admin",
                NotificationType.Error,
                NotificationCategory.Workflow,
                "Payment Failed",
                $"Payment {transaction.TransactionNumber} failed. Reason: {evt.Reason}",
                actionUrl: transaction.OrderId.HasValue ? $"/orders/{transaction.OrderId}" : null,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send in-app notification for failed payment {TransactionId}", evt.TransactionId);
        }
    }

    public async Task Handle(RefundCompletedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Processing refund completion notification for refund {RefundId}", evt.RefundId);

        var transaction = await _transactionRepository.GetByIdAsync(evt.TransactionId, ct);

        try
        {
            await _paymentHub.SendRefundStatusUpdateAsync(
                evt.RefundId,
                transaction?.TransactionNumber ?? "N/A",
                evt.TransactionId,
                "Completed",
                evt.Amount,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR refund update for {RefundId}", evt.RefundId);
        }

        try
        {
            await _notificationService.SendToRoleAsync(
                "admin",
                NotificationType.Info,
                NotificationCategory.Workflow,
                "Refund Completed",
                $"Refund of {evt.Amount} has been completed for transaction {transaction?.TransactionNumber ?? evt.TransactionId.ToString()}.",
                actionUrl: transaction?.OrderId.HasValue == true ? $"/orders/{transaction.OrderId}" : null,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send in-app notification for refund {RefundId}", evt.RefundId);
        }
    }
}
