namespace NOIR.Application.Features.Payments.DTOs;

/// <summary>
/// DTO for refund data.
/// </summary>
public record RefundDto(
    Guid Id,
    string RefundNumber,
    Guid PaymentTransactionId,
    string? GatewayRefundId,
    decimal Amount,
    string Currency,
    RefundStatus Status,
    RefundReason Reason,
    string? ReasonDetail,
    string? RequestedBy,
    string? ApprovedBy,
    DateTimeOffset? ProcessedAt,
    DateTimeOffset CreatedAt);
