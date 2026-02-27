namespace NOIR.Domain.Entities.Webhook;

/// <summary>
/// Represents an outbound webhook subscription.
/// Tenants can register URLs to receive event notifications via HTTP POST.
/// </summary>
public class WebhookSubscription : TenantAggregateRoot<Guid>
{
    private WebhookSubscription() : base() { }
    private WebhookSubscription(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Display name for this subscription.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Target URL to deliver webhook payloads to. Must be HTTPS.
    /// </summary>
    public string Url { get; private set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 secret key for signing payloads. Generated on creation.
    /// </summary>
    public string Secret { get; private set; } = string.Empty;

    /// <summary>
    /// Comma-separated event patterns (e.g., "order.*,payment.succeeded").
    /// Supports wildcard matching with '*'.
    /// </summary>
    public string EventPatterns { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this subscription is currently active and receiving events.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Maximum number of delivery retry attempts before marking as exhausted.
    /// </summary>
    public int MaxRetries { get; private set; } = 5;

    /// <summary>
    /// HTTP request timeout in seconds for webhook delivery.
    /// </summary>
    public int TimeoutSeconds { get; private set; } = 30;

    /// <summary>
    /// Optional description of this subscription's purpose.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Optional JSON string containing extra HTTP headers to include in deliveries.
    /// </summary>
    public string? CustomHeaders { get; private set; }

    /// <summary>
    /// Timestamp of the most recent delivery attempt.
    /// </summary>
    public DateTimeOffset? LastDeliveryAt { get; private set; }

    /// <summary>
    /// Current subscription status.
    /// </summary>
    public WebhookSubscriptionStatus Status { get; private set; }

    /// <summary>
    /// Delivery log entries for this subscription.
    /// </summary>
    public ICollection<WebhookDeliveryLog> DeliveryLogs { get; private set; } = new List<WebhookDeliveryLog>();

    /// <summary>
    /// Creates a new webhook subscription with a generated HMAC-SHA256 secret.
    /// </summary>
    public static WebhookSubscription Create(
        string name,
        string url,
        string eventPatterns,
        string? description = null,
        string? customHeaders = null,
        int maxRetries = 5,
        int timeoutSeconds = 30,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventPatterns);

        var subscription = new WebhookSubscription(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Url = url,
            EventPatterns = eventPatterns,
            Description = description,
            CustomHeaders = customHeaders,
            MaxRetries = maxRetries,
            TimeoutSeconds = timeoutSeconds,
            IsActive = true,
            Status = WebhookSubscriptionStatus.Active,
            Secret = GenerateSecret()
        };

        subscription.AddDomainEvent(new WebhookSubscriptionCreatedEvent(subscription.Id, name, url));

        return subscription;
    }

    /// <summary>
    /// Updates subscription configuration.
    /// </summary>
    public void Update(
        string name,
        string url,
        string eventPatterns,
        string? description,
        string? customHeaders,
        int maxRetries,
        int timeoutSeconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventPatterns);

        Name = name;
        Url = url;
        EventPatterns = eventPatterns;
        Description = description;
        CustomHeaders = customHeaders;
        MaxRetries = maxRetries;
        TimeoutSeconds = timeoutSeconds;
    }

    /// <summary>
    /// Activates the subscription to resume receiving events.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Status = WebhookSubscriptionStatus.Active;

        AddDomainEvent(new WebhookSubscriptionActivatedEvent(Id, Name));
    }

    /// <summary>
    /// Deactivates the subscription to stop receiving events.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Status = WebhookSubscriptionStatus.Inactive;

        AddDomainEvent(new WebhookSubscriptionDeactivatedEvent(Id, Name));
    }

    /// <summary>
    /// Suspends the subscription (e.g., after too many failures).
    /// </summary>
    public void Suspend()
    {
        IsActive = false;
        Status = WebhookSubscriptionStatus.Suspended;
    }

    /// <summary>
    /// Checks whether the given event type matches any of this subscription's event patterns.
    /// Supports wildcard matching (e.g., "order.*" matches "order.created").
    /// </summary>
    public bool MatchesEvent(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return false;

        var patterns = EventPatterns.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var pattern in patterns)
        {
            if (pattern == "*")
                return true;

            if (pattern.EndsWith(".*"))
            {
                var prefix = pattern[..^2];
                if (eventType.StartsWith(prefix + ".", StringComparison.OrdinalIgnoreCase) ||
                    eventType.Equals(prefix, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (eventType.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Rotates the HMAC-SHA256 secret and returns the new value.
    /// </summary>
    public string RotateSecret()
    {
        Secret = GenerateSecret();
        return Secret;
    }

    /// <summary>
    /// Records that a delivery was attempted at the current time.
    /// </summary>
    public void RecordDelivery()
    {
        LastDeliveryAt = DateTimeOffset.UtcNow;
    }

    private static string GenerateSecret()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
