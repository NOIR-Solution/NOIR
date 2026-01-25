namespace NOIR.Application.Features.Payments.DTOs;

/// <summary>
/// DTO for payment gateway configuration.
/// </summary>
public record PaymentGatewayDto(
    Guid Id,
    string Provider,
    string DisplayName,
    bool IsActive,
    int SortOrder,
    GatewayEnvironment Environment,
    bool HasCredentials,
    string? WebhookUrl,
    decimal? MinAmount,
    decimal? MaxAmount,
    string SupportedCurrencies,
    DateTimeOffset? LastHealthCheck,
    GatewayHealthStatus HealthStatus,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// DTO for gateway configuration displayed during checkout (public-facing).
/// </summary>
public record CheckoutGatewayDto(
    Guid Id,
    string Provider,
    string DisplayName,
    int SortOrder,
    decimal? MinAmount,
    decimal? MaxAmount,
    string SupportedCurrencies);
