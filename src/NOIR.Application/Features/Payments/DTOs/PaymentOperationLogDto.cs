namespace NOIR.Application.Features.Payments.DTOs;

/// <summary>
/// DTO for payment operation log data.
/// </summary>
public record PaymentOperationLogDto(
    Guid Id,
    PaymentOperationType OperationType,
    string Provider,
    Guid? PaymentTransactionId,
    string? TransactionNumber,
    Guid? RefundId,
    string CorrelationId,
    string? RequestData,
    string? ResponseData,
    int? HttpStatusCode,
    long DurationMs,
    bool Success,
    string? ErrorCode,
    string? ErrorMessage,
    string? UserId,
    string? IpAddress,
    DateTimeOffset CreatedAt);
