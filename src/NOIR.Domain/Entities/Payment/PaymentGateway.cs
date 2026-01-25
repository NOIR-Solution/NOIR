namespace NOIR.Domain.Entities.Payment;

/// <summary>
/// Per-tenant payment gateway configuration.
/// Stores encrypted credentials for gateway-agnostic payment processing.
/// </summary>
public class PaymentGateway : TenantAggregateRoot<Guid>
{
    private PaymentGateway() : base() { }
    private PaymentGateway(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Gateway provider identifier (e.g., "vnpay", "momo", "zalopay", "stripe", "cod").
    /// </summary>
    public string Provider { get; private set; } = string.Empty;

    /// <summary>
    /// Display name for the gateway (shown in checkout).
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this gateway is currently active for processing.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Display order in checkout UI.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Environment configuration (Sandbox/Production).
    /// </summary>
    public GatewayEnvironment Environment { get; private set; }

    /// <summary>
    /// AES-256 encrypted JSON credentials for the gateway.
    /// </summary>
    public string? EncryptedCredentials { get; private set; }

    /// <summary>
    /// Secret for webhook signature verification.
    /// </summary>
    public string? WebhookSecret { get; private set; }

    /// <summary>
    /// Webhook callback URL for this gateway.
    /// </summary>
    public string? WebhookUrl { get; private set; }

    /// <summary>
    /// Minimum transaction amount for this gateway.
    /// </summary>
    public decimal? MinAmount { get; private set; }

    /// <summary>
    /// Maximum transaction amount for this gateway.
    /// </summary>
    public decimal? MaxAmount { get; private set; }

    /// <summary>
    /// JSON array of supported currency codes (e.g., ["VND","USD"]).
    /// </summary>
    public string SupportedCurrencies { get; private set; } = "[]";

    /// <summary>
    /// Timestamp of the last health check.
    /// </summary>
    public DateTimeOffset? LastHealthCheck { get; private set; }

    /// <summary>
    /// Current health status of the gateway.
    /// </summary>
    public GatewayHealthStatus HealthStatus { get; private set; }

    public static PaymentGateway Create(
        string provider,
        string displayName,
        GatewayEnvironment environment,
        string? tenantId = null)
    {
        var gateway = new PaymentGateway(Guid.NewGuid(), tenantId)
        {
            Provider = provider,
            DisplayName = displayName,
            Environment = environment,
            IsActive = false,
            HealthStatus = GatewayHealthStatus.Unknown
        };
        gateway.AddDomainEvent(new PaymentGatewayCreatedEvent(gateway.Id, provider));
        return gateway;
    }

    public void Configure(string encryptedCredentials, string? webhookSecret)
    {
        EncryptedCredentials = encryptedCredentials;
        WebhookSecret = webhookSecret;
    }

    public void SetWebhookUrl(string webhookUrl)
    {
        WebhookUrl = webhookUrl;
    }

    public void SetAmountLimits(decimal? minAmount, decimal? maxAmount)
    {
        MinAmount = minAmount;
        MaxAmount = maxAmount;
    }

    public void SetSupportedCurrencies(string supportedCurrenciesJson)
    {
        SupportedCurrencies = supportedCurrenciesJson;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    public void UpdateDisplayName(string displayName)
    {
        DisplayName = displayName;
    }

    public void UpdateEnvironment(GatewayEnvironment environment)
    {
        Environment = environment;
    }

    public void UpdateCredentials(string encryptedCredentials)
    {
        EncryptedCredentials = encryptedCredentials;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateHealthStatus(GatewayHealthStatus status)
    {
        HealthStatus = status;
        LastHealthCheck = DateTimeOffset.UtcNow;
    }
}
