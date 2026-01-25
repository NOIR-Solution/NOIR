namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Abstraction for a specific payment gateway provider implementation.
/// Each provider (VNPay, MoMo, etc.) implements this interface.
/// </summary>
public interface IPaymentGatewayProvider
{
    /// <summary>
    /// Unique provider name (e.g., "vnpay", "momo", "zalopay", "stripe", "cod").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether this provider supports Cash on Delivery.
    /// </summary>
    bool SupportsCOD { get; }

    /// <summary>
    /// Initializes the provider with decrypted credentials and environment.
    /// </summary>
    Task InitializeAsync(
        Dictionary<string, string> credentials,
        GatewayEnvironment environment,
        CancellationToken ct = default);

    /// <summary>
    /// Initiates a payment with the gateway.
    /// </summary>
    Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Queries the status of a payment from the gateway.
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayTransactionId, CancellationToken ct = default);

    /// <summary>
    /// Requests a refund from the gateway.
    /// </summary>
    Task<RefundResult> RefundAsync(
        RefundRequest request, CancellationToken ct = default);

    /// <summary>
    /// Validates a webhook payload from the gateway.
    /// </summary>
    Task<WebhookValidationResult> ValidateWebhookAsync(
        WebhookPayload payload, CancellationToken ct = default);

    /// <summary>
    /// Performs a health check on the gateway.
    /// </summary>
    Task<GatewayHealthStatus> HealthCheckAsync(CancellationToken ct = default);
}

/// <summary>
/// Request to initiate a payment.
/// </summary>
public record PaymentInitiationRequest(
    Guid PaymentTransactionId,
    string TransactionNumber,
    decimal Amount,
    string Currency,
    PaymentMethod PaymentMethod,
    string ReturnUrl,
    Dictionary<string, string>? Metadata = null);

/// <summary>
/// Result from payment initiation.
/// </summary>
public record PaymentInitiationResult(
    bool Success,
    string? GatewayTransactionId,
    string? PaymentUrl,
    bool RequiresAction = false,
    string? ErrorMessage = null,
    Dictionary<string, string>? AdditionalData = null);

/// <summary>
/// Result from querying payment status.
/// </summary>
public record PaymentStatusResult(
    bool Success,
    PaymentStatus Status,
    string? GatewayTransactionId,
    string? ErrorMessage = null,
    Dictionary<string, string>? AdditionalData = null);

/// <summary>
/// Request to process a refund.
/// </summary>
public record RefundRequest(
    string GatewayTransactionId,
    string RefundNumber,
    decimal Amount,
    string Currency,
    string? Reason);

/// <summary>
/// Result from a refund operation.
/// </summary>
public record RefundResult(
    bool Success,
    string? GatewayRefundId,
    string? ErrorMessage = null);

/// <summary>
/// Webhook payload received from a gateway.
/// </summary>
public record WebhookPayload(
    string RawBody,
    string? Signature,
    Dictionary<string, string> Headers);

/// <summary>
/// Result from webhook validation.
/// </summary>
public record WebhookValidationResult(
    bool IsValid,
    string? GatewayTransactionId,
    string? EventType,
    PaymentStatus? PaymentStatus,
    string? GatewayEventId,
    string? ErrorMessage = null);
