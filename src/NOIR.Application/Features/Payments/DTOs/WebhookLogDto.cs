namespace NOIR.Application.Features.Payments.DTOs;

/// <summary>
/// DTO for webhook log data.
/// </summary>
public record WebhookLogDto(
    Guid Id,
    Guid PaymentGatewayId,
    string Provider,
    string EventType,
    string? GatewayEventId,
    bool SignatureValid,
    WebhookProcessingStatus ProcessingStatus,
    string? ProcessingError,
    int RetryCount,
    Guid? PaymentTransactionId,
    string? IpAddress,
    DateTimeOffset CreatedAt);
