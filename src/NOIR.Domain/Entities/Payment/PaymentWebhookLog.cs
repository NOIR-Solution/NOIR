namespace NOIR.Domain.Entities.Payment;

/// <summary>
/// Log entry for payment webhook events received from gateways.
/// Used for audit trail, debugging, and retry processing.
/// </summary>
public class PaymentWebhookLog : TenantAggregateRoot<Guid>
{
    private PaymentWebhookLog() : base() { }
    private PaymentWebhookLog(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Gateway that sent the webhook.
    /// </summary>
    public Guid PaymentGatewayId { get; private set; }

    /// <summary>
    /// Provider name (denormalized for queries).
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Type of webhook event (e.g., "payment.success", "refund.completed").
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Gateway-specific event identifier for deduplication.
    /// </summary>
    public string? GatewayEventId { get; private set; }

    /// <summary>
    /// Raw webhook request body.
    /// </summary>
    public string RequestBody { get; private set; } = string.Empty;

    /// <summary>
    /// Webhook request headers as JSON.
    /// </summary>
    public string? RequestHeaders { get; private set; }

    /// <summary>
    /// Signature value provided in the webhook.
    /// </summary>
    public string? SignatureValue { get; private set; }

    /// <summary>
    /// Whether the webhook signature was validated successfully.
    /// </summary>
    public bool SignatureValid { get; private set; }

    /// <summary>
    /// Current processing status of the webhook.
    /// </summary>
    public WebhookProcessingStatus ProcessingStatus { get; private set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ProcessingError { get; private set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Associated payment transaction (if resolved).
    /// </summary>
    public Guid? PaymentTransactionId { get; private set; }

    /// <summary>
    /// IP address the webhook was received from.
    /// </summary>
    public string? IpAddress { get; private set; }

    public static PaymentWebhookLog Create(
        Guid paymentGatewayId,
        string provider,
        string eventType,
        string requestBody,
        string? tenantId = null)
    {
        return new PaymentWebhookLog(Guid.NewGuid(), tenantId)
        {
            PaymentGatewayId = paymentGatewayId,
            Provider = provider,
            EventType = eventType,
            RequestBody = requestBody,
            ProcessingStatus = WebhookProcessingStatus.Received
        };
    }

    public void SetRequestDetails(string? headers, string? signatureValue, string? ipAddress)
    {
        RequestHeaders = headers;
        SignatureValue = signatureValue;
        IpAddress = ipAddress;
    }

    public void SetGatewayEventId(string gatewayEventId)
    {
        GatewayEventId = gatewayEventId;
    }

    public void MarkSignatureValid(bool isValid)
    {
        SignatureValid = isValid;
    }

    public void MarkAsProcessing()
    {
        ProcessingStatus = WebhookProcessingStatus.Processing;
    }

    public void MarkAsProcessed(Guid? paymentTransactionId = null)
    {
        ProcessingStatus = WebhookProcessingStatus.Processed;
        PaymentTransactionId = paymentTransactionId;
    }

    public void MarkAsFailed(string error)
    {
        ProcessingStatus = WebhookProcessingStatus.Failed;
        ProcessingError = error;
        RetryCount++;
    }

    public void MarkAsSkipped(string reason)
    {
        ProcessingStatus = WebhookProcessingStatus.Skipped;
        ProcessingError = reason;
    }
}
