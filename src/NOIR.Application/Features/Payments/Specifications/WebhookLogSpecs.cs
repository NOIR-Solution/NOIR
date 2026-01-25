namespace NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Get webhook logs for a specific payment transaction.
/// </summary>
public sealed class WebhookLogsByPaymentSpec : Specification<PaymentWebhookLog>
{
    public WebhookLogsByPaymentSpec(Guid paymentTransactionId)
    {
        Query.Where(w => w.PaymentTransactionId == paymentTransactionId)
             .OrderByDescending(w => (object)w.CreatedAt)
             .TagWith("WebhookLogsByPayment");
    }
}

/// <summary>
/// Get unprocessed/failed webhook logs for retry.
/// </summary>
public sealed class UnprocessedWebhooksSpec : Specification<PaymentWebhookLog>
{
    public UnprocessedWebhooksSpec(int maxRetries = 5)
    {
        Query.Where(w => w.ProcessingStatus == WebhookProcessingStatus.Failed)
             .Where(w => w.RetryCount < maxRetries)
             .AsTracking()
             .OrderBy(w => (object)w.CreatedAt)
             .TagWith("UnprocessedWebhooks");
    }
}

/// <summary>
/// Get webhook logs with filtering and pagination.
/// </summary>
public sealed class WebhookLogsSpec : Specification<PaymentWebhookLog>
{
    public WebhookLogsSpec(
        string? provider = null,
        WebhookProcessingStatus? status = null,
        int? skip = null,
        int? take = null)
    {
        Query.Where(w => string.IsNullOrEmpty(provider) || w.Provider == provider)
             .Where(w => status == null || w.ProcessingStatus == status)
             .OrderByDescending(w => (object)w.CreatedAt)
             .TagWith("GetWebhookLogs");

        if (skip.HasValue)
            Query.Skip(skip.Value);
        if (take.HasValue)
            Query.Take(take.Value);
    }
}

/// <summary>
/// Get webhook log by gateway event ID (for deduplication).
/// </summary>
public sealed class WebhookLogByGatewayEventIdSpec : Specification<PaymentWebhookLog>
{
    public WebhookLogByGatewayEventIdSpec(string gatewayEventId)
    {
        Query.Where(w => w.GatewayEventId == gatewayEventId)
             .TagWith("WebhookLogByGatewayEventId");
    }
}

/// <summary>
/// Get refunds for a payment transaction.
/// </summary>
public sealed class RefundsByPaymentSpec : Specification<Refund>
{
    public RefundsByPaymentSpec(Guid paymentTransactionId)
    {
        Query.Where(r => r.PaymentTransactionId == paymentTransactionId)
             .OrderByDescending(r => (object)r.CreatedAt)
             .TagWith("RefundsByPayment");
    }
}

/// <summary>
/// Get pending refunds awaiting processing.
/// </summary>
public sealed class PendingRefundsSpec : Specification<Refund>
{
    public PendingRefundsSpec()
    {
        Query.Where(r => r.Status == RefundStatus.Approved)
             .Include(r => r.PaymentTransaction!)
             .AsTracking()
             .OrderBy(r => (object)r.CreatedAt)
             .TagWith("PendingRefunds");
    }
}

/// <summary>
/// Get refund by ID (read-only).
/// </summary>
public sealed class RefundByIdSpec : Specification<Refund>
{
    public RefundByIdSpec(Guid id)
    {
        Query.Where(r => r.Id == id)
             .Include(r => r.PaymentTransaction!)
             .TagWith("RefundById");
    }
}

/// <summary>
/// Get refund by ID for update (with tracking).
/// </summary>
public sealed class RefundByIdForUpdateSpec : Specification<Refund>
{
    public RefundByIdForUpdateSpec(Guid id)
    {
        Query.Where(r => r.Id == id)
             .AsTracking()
             .TagWith("RefundByIdForUpdate");
    }
}
