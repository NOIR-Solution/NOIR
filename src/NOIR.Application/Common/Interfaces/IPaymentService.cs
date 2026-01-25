namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// High-level payment service abstraction.
/// Orchestrates payment lifecycle operations.
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Generates a unique transaction number.
    /// </summary>
    string GenerateTransactionNumber();

    /// <summary>
    /// Generates a unique refund number.
    /// </summary>
    string GenerateRefundNumber();

    /// <summary>
    /// Gets the current status of a payment from the gateway.
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(
        Guid paymentTransactionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes an approved refund through the gateway.
    /// </summary>
    Task<RefundResult> ProcessRefundAsync(
        Guid refundId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires stale pending payments.
    /// </summary>
    Task ExpireStalePaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a payment gateway is healthy.
    /// </summary>
    Task<bool> IsGatewayHealthyAsync(
        string provider,
        CancellationToken cancellationToken = default);
}
