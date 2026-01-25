namespace NOIR.Domain.Enums;

/// <summary>
/// Types of payment operations logged for debugging.
/// </summary>
public enum PaymentOperationType
{
    /// <summary>
    /// Initiating a new payment with gateway.
    /// </summary>
    InitiatePayment,

    /// <summary>
    /// Querying payment status from gateway.
    /// </summary>
    GetPaymentStatus,

    /// <summary>
    /// Validating webhook signature.
    /// </summary>
    ValidateWebhook,

    /// <summary>
    /// Processing webhook payload.
    /// </summary>
    ProcessWebhook,

    /// <summary>
    /// Initiating a refund with gateway.
    /// </summary>
    InitiateRefund,

    /// <summary>
    /// Querying refund status from gateway.
    /// </summary>
    GetRefundStatus,

    /// <summary>
    /// Testing gateway connection/credentials.
    /// </summary>
    TestConnection,

    /// <summary>
    /// Cancelling a payment.
    /// </summary>
    CancelPayment,

    /// <summary>
    /// Confirming COD collection.
    /// </summary>
    ConfirmCodCollection,

    /// <summary>
    /// Gateway health check.
    /// </summary>
    HealthCheck,

    /// <summary>
    /// Generating payment URL/QR code.
    /// </summary>
    GeneratePaymentUrl,

    /// <summary>
    /// Capturing an authorized payment.
    /// </summary>
    CapturePayment
}
