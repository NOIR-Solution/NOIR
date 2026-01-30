namespace NOIR.Application.Features.Shipping.DTOs;

/// <summary>
/// DTO for shipping provider configuration (admin view).
/// </summary>
public record ShippingProviderDto(
    Guid Id,
    ShippingProviderCode ProviderCode,
    string ProviderName,
    string DisplayName,
    bool IsActive,
    int SortOrder,
    GatewayEnvironment Environment,
    bool HasCredentials,
    string? WebhookUrl,
    string? ApiBaseUrl,
    string? TrackingUrlTemplate,
    string SupportedServices,
    int? MinWeightGrams,
    int? MaxWeightGrams,
    decimal? MinCodAmount,
    decimal? MaxCodAmount,
    bool SupportsCod,
    bool SupportsInsurance,
    DateTimeOffset? LastHealthCheck,
    ShippingProviderHealthStatus HealthStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// DTO for shipping provider displayed during checkout (public-facing).
/// </summary>
public record CheckoutShippingProviderDto(
    Guid Id,
    ShippingProviderCode ProviderCode,
    string ProviderName,
    string DisplayName,
    int SortOrder,
    string SupportedServices,
    bool SupportsCod,
    bool SupportsInsurance);

/// <summary>
/// DTO for shipping provider list (summary view).
/// </summary>
public record ShippingProviderListDto(
    Guid Id,
    ShippingProviderCode ProviderCode,
    string ProviderName,
    string DisplayName,
    bool IsActive,
    int SortOrder,
    GatewayEnvironment Environment,
    ShippingProviderHealthStatus HealthStatus);
