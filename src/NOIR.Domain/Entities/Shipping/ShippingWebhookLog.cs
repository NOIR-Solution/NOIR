namespace NOIR.Domain.Entities.Shipping;

/// <summary>
/// Log of incoming webhooks from shipping providers.
/// Used for debugging and replay if processing fails.
/// </summary>
public class ShippingWebhookLog : Entity<Guid>
{
    private ShippingWebhookLog() : base() { }
    private ShippingWebhookLog(Guid id) : base(id) { }

    /// <summary>
    /// Provider code (e.g., "GHTK", "GHN").
    /// </summary>
    public ShippingProviderCode ProviderCode { get; private set; }

    /// <summary>
    /// Tracking number extracted from payload (if available).
    /// </summary>
    public string? TrackingNumber { get; private set; }

    /// <summary>
    /// HTTP method used.
    /// </summary>
    public string HttpMethod { get; private set; } = "POST";

    /// <summary>
    /// Endpoint path that received the webhook.
    /// </summary>
    public string Endpoint { get; private set; } = string.Empty;

    /// <summary>
    /// Request headers as JSON.
    /// </summary>
    public string? HeadersJson { get; private set; }

    /// <summary>
    /// Raw request body.
    /// </summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>
    /// Signature from provider (for verification).
    /// </summary>
    public string? Signature { get; private set; }

    /// <summary>
    /// Whether processing succeeded.
    /// </summary>
    public bool ProcessedSuccessfully { get; private set; }

    /// <summary>
    /// Error message if processing failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Number of processing attempts.
    /// </summary>
    public int ProcessingAttempts { get; private set; }

    /// <summary>
    /// When we received the webhook.
    /// </summary>
    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>
    /// When processing completed (success or final failure).
    /// </summary>
    public DateTimeOffset? ProcessedAt { get; private set; }

    public static ShippingWebhookLog Create(
        ShippingProviderCode providerCode,
        string endpoint,
        string body,
        string? trackingNumber = null,
        string? headersJson = null,
        string? signature = null,
        string httpMethod = "POST")
    {
        return new ShippingWebhookLog(Guid.NewGuid())
        {
            ProviderCode = providerCode,
            Endpoint = endpoint,
            Body = body,
            TrackingNumber = trackingNumber,
            HeadersJson = headersJson,
            Signature = signature,
            HttpMethod = httpMethod,
            ProcessedSuccessfully = false,
            ProcessingAttempts = 0,
            ReceivedAt = DateTimeOffset.UtcNow
        };
    }

    public void MarkAsProcessed()
    {
        ProcessedSuccessfully = true;
        ProcessedAt = DateTimeOffset.UtcNow;
        ProcessingAttempts++;
    }

    public void MarkAsFailed(string errorMessage)
    {
        ProcessedSuccessfully = false;
        ErrorMessage = errorMessage;
        ProcessedAt = DateTimeOffset.UtcNow;
        ProcessingAttempts++;
    }

    public void SetTrackingNumber(string trackingNumber)
    {
        TrackingNumber = trackingNumber;
    }
}
