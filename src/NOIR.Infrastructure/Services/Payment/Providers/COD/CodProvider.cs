using Microsoft.Extensions.Options;
using NOIR.Application.Common.Interfaces;
using NOIR.Application.Common.Settings;
using NOIR.Domain.Enums;

namespace NOIR.Infrastructure.Services.Payment.Providers.COD;

/// <summary>
/// Cash on Delivery (COD) payment provider implementation.
/// This provider doesn't interact with external services - COD payments are tracked locally
/// and confirmed by admin when the courier collects cash.
/// </summary>
public class CodProvider : IPaymentGatewayProvider
{
    private readonly ILogger<CodProvider> _logger;
    private readonly PaymentSettings _paymentSettings;

    private decimal _maxAmount;
    private bool _isEnabled;

    public CodProvider(
        IOptions<PaymentSettings> paymentSettings,
        ILogger<CodProvider> logger)
    {
        _logger = logger;
        _paymentSettings = paymentSettings.Value;
        _maxAmount = _paymentSettings.COD.MaxAmount;
        _isEnabled = _paymentSettings.COD.Enabled;
    }

    public string ProviderName => "cod";

    public bool SupportsCOD => true;

    public Task InitializeAsync(
        Dictionary<string, string> credentials,
        GatewayEnvironment environment,
        CancellationToken ct = default)
    {
        // COD may have per-tenant configuration for max amount
        if (credentials.TryGetValue("MaxAmount", out var maxAmountStr) &&
            decimal.TryParse(maxAmountStr, out var maxAmount))
        {
            _maxAmount = maxAmount;
        }

        if (credentials.TryGetValue("Enabled", out var enabledStr) &&
            bool.TryParse(enabledStr, out var enabled))
        {
            _isEnabled = enabled;
        }

        return Task.CompletedTask;
    }

    public Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            if (!_isEnabled)
            {
                return Task.FromResult(new PaymentInitiationResult(
                    Success: false,
                    GatewayTransactionId: null,
                    PaymentUrl: null,
                    RequiresAction: false,
                    ErrorMessage: "COD payment is not enabled"));
            }

            if (request.Amount > _maxAmount)
            {
                return Task.FromResult(new PaymentInitiationResult(
                    Success: false,
                    GatewayTransactionId: null,
                    PaymentUrl: null,
                    RequiresAction: false,
                    ErrorMessage: $"COD amount exceeds maximum limit of {_maxAmount:N0} {request.Currency}"));
            }

            // COD doesn't need external initiation - mark as pending and redirect to success
            var codTransactionId = $"COD-{request.TransactionNumber}";

            _logger.LogInformation(
                "COD payment initiated for transaction {TransactionNumber}, Amount: {Amount} {Currency}",
                request.TransactionNumber,
                request.Amount,
                request.Currency);

            return Task.FromResult(new PaymentInitiationResult(
                Success: true,
                GatewayTransactionId: codTransactionId,
                PaymentUrl: request.ReturnUrl, // Redirect directly to success page
                RequiresAction: false, // No user action needed at payment gateway
                ErrorMessage: null,
                AdditionalData: new Dictionary<string, string>
                {
                    ["payment_method"] = "cod",
                    ["max_amount"] = _maxAmount.ToString("F0"),
                    ["note"] = "Payment will be collected upon delivery"
                }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate COD payment for {TransactionNumber}", request.TransactionNumber);

            return Task.FromResult(new PaymentInitiationResult(
                Success: false,
                GatewayTransactionId: null,
                PaymentUrl: null,
                RequiresAction: false,
                ErrorMessage: ex.Message));
        }
    }

    public Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayTransactionId,
        CancellationToken ct = default)
    {
        // COD status is managed internally, not queried from external gateway
        // This method returns Pending as the transaction needs to wait for delivery

        return Task.FromResult(new PaymentStatusResult(
            Success: true,
            Status: PaymentStatus.CodPending,
            GatewayTransactionId: gatewayTransactionId,
            ErrorMessage: null,
            AdditionalData: new Dictionary<string, string>
            {
                ["message"] = "COD payment pending delivery and collection"
            }));
    }

    public Task<RefundResult> RefundAsync(RefundRequest request, CancellationToken ct = default)
    {
        // COD refunds are handled differently - typically the customer returns the item
        // and the refund is processed manually. For collected COD, this is a manual process.

        _logger.LogInformation(
            "COD refund requested for transaction {TransactionId}, Amount: {Amount}",
            request.GatewayTransactionId,
            request.Amount);

        // Generate a refund tracking number
        var refundId = $"COD-REFUND-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

        return Task.FromResult(new RefundResult(
            Success: true,
            GatewayRefundId: refundId,
            ErrorMessage: null));
    }

    public Task<WebhookValidationResult> ValidateWebhookAsync(
        WebhookPayload payload,
        CancellationToken ct = default)
    {
        // COD doesn't have external webhooks
        return Task.FromResult(new WebhookValidationResult(
            IsValid: false,
            GatewayTransactionId: null,
            EventType: null,
            PaymentStatus: null,
            GatewayEventId: null,
            ErrorMessage: "COD does not support webhooks"));
    }

    public Task<GatewayHealthStatus> HealthCheckAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_isEnabled
            ? GatewayHealthStatus.Healthy
            : GatewayHealthStatus.Unhealthy);
    }
}
