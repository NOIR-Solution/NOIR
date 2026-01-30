namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Abstraction for shipping provider implementations.
/// Each provider (GHTK, GHN, etc.) implements this interface.
/// </summary>
public interface IShippingProvider
{
    /// <summary>
    /// Provider code identifier.
    /// </summary>
    ShippingProviderCode ProviderCode { get; }

    /// <summary>
    /// Display name of the provider.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Calculate shipping rates for a given route and package.
    /// </summary>
    Task<Result<List<ShippingRateDto>>> CalculateRatesAsync(
        CalculateShippingRatesRequest request,
        ShippingProvider providerConfig,
        CancellationToken ct = default);

    /// <summary>
    /// Create a shipping order with the provider.
    /// </summary>
    Task<Result<ProviderShippingOrderResult>> CreateOrderAsync(
        CreateShippingOrderRequest request,
        ShippingProvider providerConfig,
        CancellationToken ct = default);

    /// <summary>
    /// Get order details from the provider.
    /// </summary>
    Task<Result<ProviderOrderDetails>> GetOrderAsync(
        string trackingNumber,
        ShippingProvider providerConfig,
        CancellationToken ct = default);

    /// <summary>
    /// Cancel a shipping order.
    /// </summary>
    Task<Result> CancelOrderAsync(
        string trackingNumber,
        ShippingProvider providerConfig,
        CancellationToken ct = default);

    /// <summary>
    /// Get tracking information for a shipment.
    /// </summary>
    Task<Result<ProviderTrackingInfo>> GetTrackingAsync(
        string trackingNumber,
        ShippingProvider providerConfig,
        CancellationToken ct = default);

    /// <summary>
    /// Parse and validate a webhook payload from this provider.
    /// </summary>
    Result<ShippingWebhookPayload> ParseWebhook(
        string rawPayload,
        string? signature,
        ShippingProvider providerConfig);

    /// <summary>
    /// Get available service types for a route.
    /// </summary>
    Task<Result<List<ProviderServiceType>>> GetServiceTypesAsync(
        string originProvinceCode,
        string destinationProvinceCode,
        ShippingProvider providerConfig,
        CancellationToken ct = default);

    /// <summary>
    /// Check if the provider API is healthy.
    /// </summary>
    Task<Result<ShippingProviderHealthStatus>> HealthCheckAsync(
        ShippingProvider providerConfig,
        CancellationToken ct = default);
}

/// <summary>
/// Result from creating an order with a provider.
/// </summary>
public record ProviderShippingOrderResult(
    string TrackingNumber,
    string? ProviderOrderId,
    string? LabelUrl,
    decimal ShippingFee,
    decimal? CodFee,
    decimal? InsuranceFee,
    DateTimeOffset? EstimatedDeliveryDate,
    string? RawResponse = null);

/// <summary>
/// Order details from a provider.
/// </summary>
public record ProviderOrderDetails(
    string TrackingNumber,
    string? ProviderOrderId,
    ShippingStatus Status,
    string StatusDescription,
    string? CurrentLocation,
    DateTimeOffset? EstimatedDeliveryDate,
    DateTimeOffset? ActualDeliveryDate);

/// <summary>
/// Tracking information from a provider.
/// </summary>
public record ProviderTrackingInfo(
    string TrackingNumber,
    ShippingStatus CurrentStatus,
    string StatusDescription,
    string? CurrentLocation,
    DateTimeOffset? EstimatedDeliveryDate,
    DateTimeOffset? ActualDeliveryDate,
    List<ProviderTrackingEvent> Events);

/// <summary>
/// A tracking event from a provider.
/// </summary>
public record ProviderTrackingEvent(
    string EventType,
    ShippingStatus Status,
    string Description,
    string? Location,
    DateTimeOffset EventDate);

/// <summary>
/// Service type offered by a provider.
/// </summary>
public record ProviderServiceType(
    string Code,
    string Name,
    string? Description,
    int EstimatedDaysMin,
    int EstimatedDaysMax);
