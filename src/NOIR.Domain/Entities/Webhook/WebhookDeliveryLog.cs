namespace NOIR.Domain.Entities.Webhook;

/// <summary>
/// Records the result of a single webhook delivery attempt.
/// Child entity of WebhookSubscription (not an aggregate root).
/// </summary>
public class WebhookDeliveryLog : TenantEntity<Guid>
{
    private WebhookDeliveryLog() : base() { }

    /// <summary>
    /// Parent subscription that triggered this delivery.
    /// </summary>
    public Guid WebhookSubscriptionId { get; private set; }

    /// <summary>
    /// The event type that triggered this delivery (e.g., "order.created").
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Unique identifier for the originating domain event.
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// The URL the request was sent to.
    /// </summary>
    public string RequestUrl { get; private set; } = string.Empty;

    /// <summary>
    /// The JSON request body that was sent.
    /// </summary>
    public string RequestBody { get; private set; } = string.Empty;

    /// <summary>
    /// Optional JSON string of request headers sent with the delivery.
    /// </summary>
    public string? RequestHeaders { get; private set; }

    /// <summary>
    /// Response body received from the target URL.
    /// </summary>
    public string? ResponseBody { get; private set; }

    /// <summary>
    /// HTTP status code returned by the target URL.
    /// </summary>
    public int? ResponseStatusCode { get; private set; }

    /// <summary>
    /// Response headers returned by the target URL.
    /// </summary>
    public string? ResponseHeaders { get; private set; }

    /// <summary>
    /// Current delivery status.
    /// </summary>
    public WebhookDeliveryStatus Status { get; private set; }

    /// <summary>
    /// Current attempt number (starts at 1).
    /// </summary>
    public int AttemptNumber { get; private set; }

    /// <summary>
    /// Scheduled time for the next retry attempt, if applicable.
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; private set; }

    /// <summary>
    /// Error message if the delivery failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Duration of the HTTP request in milliseconds.
    /// </summary>
    public long? DurationMs { get; private set; }

    /// <summary>
    /// Navigation property to the parent subscription.
    /// </summary>
    public virtual WebhookSubscription? Subscription { get; private set; }

    /// <summary>
    /// Creates a new delivery log entry in Pending status.
    /// </summary>
    public static WebhookDeliveryLog Create(
        Guid subscriptionId,
        string eventType,
        Guid eventId,
        string requestUrl,
        string requestBody,
        string? requestHeaders = null,
        string? tenantId = null)
    {
        return new WebhookDeliveryLog(Guid.NewGuid(), tenantId)
        {
            WebhookSubscriptionId = subscriptionId,
            EventType = eventType,
            EventId = eventId,
            RequestUrl = requestUrl,
            RequestBody = requestBody,
            RequestHeaders = requestHeaders,
            Status = WebhookDeliveryStatus.Pending,
            AttemptNumber = 1
        };
    }

    private WebhookDeliveryLog(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Records a successful delivery.
    /// </summary>
    public void RecordSuccess(int statusCode, string? responseBody, string? responseHeaders, long durationMs)
    {
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
        ResponseHeaders = responseHeaders;
        DurationMs = durationMs;
        Status = WebhookDeliveryStatus.Succeeded;
        NextRetryAt = null;
    }

    /// <summary>
    /// Records a failed delivery attempt. If nextRetryAt is null, marks as Exhausted (no more retries).
    /// </summary>
    public void RecordFailure(
        int? statusCode,
        string? responseBody,
        string? responseHeaders,
        string? errorMessage,
        long durationMs,
        DateTimeOffset? nextRetryAt)
    {
        ResponseStatusCode = statusCode;
        ResponseBody = responseBody;
        ResponseHeaders = responseHeaders;
        ErrorMessage = errorMessage;
        DurationMs = durationMs;
        AttemptNumber++;
        NextRetryAt = nextRetryAt;
        Status = nextRetryAt.HasValue ? WebhookDeliveryStatus.Retrying : WebhookDeliveryStatus.Exhausted;
    }
}
