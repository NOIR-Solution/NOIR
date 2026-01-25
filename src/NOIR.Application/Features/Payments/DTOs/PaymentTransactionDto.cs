namespace NOIR.Application.Features.Payments.DTOs;

/// <summary>
/// DTO for payment transaction data.
/// </summary>
public record PaymentTransactionDto(
    Guid Id,
    string TransactionNumber,
    string? GatewayTransactionId,
    Guid PaymentGatewayId,
    string Provider,
    Guid? OrderId,
    Guid? CustomerId,
    decimal Amount,
    string Currency,
    decimal? GatewayFee,
    decimal? NetAmount,
    PaymentStatus Status,
    string? FailureReason,
    PaymentMethod PaymentMethod,
    string? PaymentMethodDetail,
    DateTimeOffset? PaidAt,
    DateTimeOffset? ExpiresAt,
    string? CodCollectorName,
    DateTimeOffset? CodCollectedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// DTO for payment transaction list items (lighter than full DTO).
/// </summary>
public record PaymentTransactionListDto(
    Guid Id,
    string TransactionNumber,
    string Provider,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    PaymentMethod PaymentMethod,
    DateTimeOffset? PaidAt,
    DateTimeOffset CreatedAt);
