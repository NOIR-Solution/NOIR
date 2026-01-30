namespace NOIR.Domain.Entities.Shipping;

/// <summary>
/// Per-tenant shipping provider configuration.
/// Stores encrypted credentials for provider-agnostic shipping operations.
/// Follows the same pattern as PaymentGateway for consistency.
/// </summary>
public class ShippingProvider : TenantAggregateRoot<Guid>
{
    private ShippingProvider() : base() { }
    private ShippingProvider(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Shipping provider code (e.g., GHTK, GHN, JTExpress).
    /// </summary>
    public ShippingProviderCode ProviderCode { get; private set; }

    /// <summary>
    /// Display name for the provider (shown in checkout).
    /// </summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Provider's official name.
    /// </summary>
    public string ProviderName { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this provider is currently active for shipping.
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
    /// AES-256 encrypted JSON credentials for the provider.
    /// </summary>
    public string? EncryptedCredentials { get; private set; }

    /// <summary>
    /// Secret for webhook signature verification.
    /// </summary>
    public string? WebhookSecret { get; private set; }

    /// <summary>
    /// Webhook callback URL for this provider.
    /// </summary>
    public string? WebhookUrl { get; private set; }

    /// <summary>
    /// JSON array of supported service types (e.g., ["Standard","Express"]).
    /// </summary>
    public string SupportedServices { get; private set; } = "[]";

    /// <summary>
    /// Minimum order weight in grams for this provider.
    /// </summary>
    public int? MinWeightGrams { get; private set; }

    /// <summary>
    /// Maximum order weight in grams for this provider.
    /// </summary>
    public int? MaxWeightGrams { get; private set; }

    /// <summary>
    /// Minimum COD amount for this provider.
    /// </summary>
    public decimal? MinCodAmount { get; private set; }

    /// <summary>
    /// Maximum COD amount for this provider.
    /// </summary>
    public decimal? MaxCodAmount { get; private set; }

    /// <summary>
    /// Whether this provider supports Cash on Delivery.
    /// </summary>
    public bool SupportsCod { get; private set; }

    /// <summary>
    /// Whether this provider supports insurance.
    /// </summary>
    public bool SupportsInsurance { get; private set; }

    /// <summary>
    /// Base URL for the provider's API.
    /// </summary>
    public string? ApiBaseUrl { get; private set; }

    /// <summary>
    /// Tracking URL template (use {trackingNumber} placeholder).
    /// </summary>
    public string? TrackingUrlTemplate { get; private set; }

    /// <summary>
    /// Timestamp of the last health check.
    /// </summary>
    public DateTimeOffset? LastHealthCheck { get; private set; }

    /// <summary>
    /// Current health status of the provider.
    /// </summary>
    public ShippingProviderHealthStatus HealthStatus { get; private set; }

    /// <summary>
    /// Provider-specific metadata as JSON.
    /// </summary>
    public string? Metadata { get; private set; }

    public static ShippingProvider Create(
        ShippingProviderCode providerCode,
        string displayName,
        string providerName,
        GatewayEnvironment environment,
        string? tenantId = null)
    {
        var provider = new ShippingProvider(Guid.NewGuid(), tenantId)
        {
            ProviderCode = providerCode,
            DisplayName = displayName,
            ProviderName = providerName,
            Environment = environment,
            IsActive = false,
            HealthStatus = ShippingProviderHealthStatus.Unknown,
            SupportsCod = true,
            SupportsInsurance = false
        };
        provider.AddDomainEvent(new ShippingProviderCreatedEvent(provider.Id, providerCode));
        return provider;
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

    public void SetApiBaseUrl(string apiBaseUrl)
    {
        ApiBaseUrl = apiBaseUrl;
    }

    public void SetTrackingUrlTemplate(string trackingUrlTemplate)
    {
        TrackingUrlTemplate = trackingUrlTemplate;
    }

    public void SetWeightLimits(int? minWeightGrams, int? maxWeightGrams)
    {
        MinWeightGrams = minWeightGrams;
        MaxWeightGrams = maxWeightGrams;
    }

    public void SetCodLimits(decimal? minCodAmount, decimal? maxCodAmount)
    {
        MinCodAmount = minCodAmount;
        MaxCodAmount = maxCodAmount;
    }

    public void SetSupportedServices(string supportedServicesJson)
    {
        SupportedServices = supportedServicesJson;
    }

    public void SetCodSupport(bool supportsCod)
    {
        SupportsCod = supportsCod;
    }

    public void SetInsuranceSupport(bool supportsInsurance)
    {
        SupportsInsurance = supportsInsurance;
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

    public void SetMetadata(string? metadata)
    {
        Metadata = metadata;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateHealthStatus(ShippingProviderHealthStatus status)
    {
        HealthStatus = status;
        LastHealthCheck = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Generates the tracking URL for a given tracking number.
    /// </summary>
    public string? GetTrackingUrl(string trackingNumber)
    {
        if (string.IsNullOrEmpty(TrackingUrlTemplate))
            return null;

        return TrackingUrlTemplate.Replace("{trackingNumber}", trackingNumber);
    }
}
