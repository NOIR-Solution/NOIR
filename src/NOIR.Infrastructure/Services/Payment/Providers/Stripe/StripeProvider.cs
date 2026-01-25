using Stripe;

namespace NOIR.Infrastructure.Services.Payment.Providers.Stripe;

/// <summary>
/// Stripe payment gateway provider implementation.
/// Supports Payment Intents, 3D Secure, multi-currency, subscriptions.
/// </summary>
public class StripeProvider : IPaymentGatewayProvider
{
    private readonly IStripeClient _client;
    private readonly ILogger<StripeProvider> _logger;
    private readonly StripeSettings _defaultSettings;

    // Per-tenant credentials (populated via InitializeAsync)
    private string _secretKey = string.Empty;
    private string _webhookSecret = string.Empty;
    private GatewayEnvironment _environment = GatewayEnvironment.Sandbox;

    public StripeProvider(
        IStripeClient client,
        IOptions<StripeSettings> settings,
        ILogger<StripeProvider> logger)
    {
        _client = client;
        _logger = logger;
        _defaultSettings = settings.Value;
    }

    public string ProviderName => "stripe";

    public bool SupportsCOD => false;

    public Task InitializeAsync(
        Dictionary<string, string> credentials,
        GatewayEnvironment environment,
        CancellationToken ct = default)
    {
        _environment = environment;

        // Use credentials from database or fall back to config
        _secretKey = credentials.GetValueOrDefault("SecretKey", _defaultSettings.SecretKey);
        _webhookSecret = credentials.GetValueOrDefault("WebhookSecret", _defaultSettings.WebhookSecret);

        // Initialize the client with tenant-specific key (uses per-request RequestOptions)
        // NOTE: Do NOT set StripeConfiguration.ApiKey globally - it's not multi-tenant safe
        _client.Initialize(_secretKey);

        return Task.CompletedTask;
    }

    public async Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            // Stripe amounts are in smallest currency unit (cents for USD)
            var amount = GetStripeAmount(request.Amount, request.Currency);

            var metadata = new Dictionary<string, string>
            {
                ["transaction_number"] = request.TransactionNumber,
                ["payment_transaction_id"] = request.PaymentTransactionId.ToString()
            };

            if (request.Metadata != null)
            {
                foreach (var kvp in request.Metadata)
                {
                    metadata[kvp.Key] = kvp.Value;
                }
            }

            var paymentIntent = await _client.CreatePaymentIntentAsync(
                amount,
                request.Currency,
                $"Payment for {request.TransactionNumber}",
                metadata,
                ct);

            _logger.LogInformation(
                "Stripe PaymentIntent created: {PaymentIntentId} for transaction {TransactionNumber}, Amount: {Amount} {Currency}",
                paymentIntent.Id,
                request.TransactionNumber,
                request.Amount,
                request.Currency);

            return new PaymentInitiationResult(
                Success: true,
                GatewayTransactionId: paymentIntent.Id,
                PaymentUrl: null, // Stripe uses client-side SDK, not redirect
                RequiresAction: paymentIntent.Status == "requires_action",
                ErrorMessage: null,
                AdditionalData: new Dictionary<string, string>
                {
                    ["client_secret"] = paymentIntent.ClientSecret,
                    ["payment_intent_id"] = paymentIntent.Id,
                    ["publishable_key"] = _defaultSettings.PublishableKey
                });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to initiate Stripe payment for {TransactionNumber}", request.TransactionNumber);

            return new PaymentInitiationResult(
                Success: false,
                GatewayTransactionId: null,
                PaymentUrl: null,
                RequiresAction: false,
                ErrorMessage: ex.Message);
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(
        string gatewayTransactionId,
        CancellationToken ct = default)
    {
        try
        {
            var paymentIntent = await _client.GetPaymentIntentAsync(gatewayTransactionId, ct);

            if (paymentIntent == null)
            {
                return new PaymentStatusResult(
                    Success: false,
                    Status: PaymentStatus.Pending,
                    GatewayTransactionId: gatewayTransactionId,
                    ErrorMessage: "PaymentIntent not found");
            }

            var status = MapStripeStatusToPaymentStatus(paymentIntent.Status);

            return new PaymentStatusResult(
                Success: true,
                Status: status,
                GatewayTransactionId: paymentIntent.Id,
                AdditionalData: new Dictionary<string, string>
                {
                    ["stripe_status"] = paymentIntent.Status,
                    ["amount"] = paymentIntent.Amount.ToString(),
                    ["currency"] = paymentIntent.Currency
                });
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe payment status for {TransactionId}", gatewayTransactionId);

            return new PaymentStatusResult(
                Success: false,
                Status: PaymentStatus.Pending,
                GatewayTransactionId: gatewayTransactionId,
                ErrorMessage: ex.Message);
        }
    }

    public async Task<Application.Common.Interfaces.RefundResult> RefundAsync(
        Application.Common.Interfaces.RefundRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var amount = GetStripeAmount(request.Amount, request.Currency);

            var refund = await _client.CreateRefundAsync(
                request.GatewayTransactionId,
                amount,
                request.Reason,
                ct);

            if (refund == null)
            {
                return new Application.Common.Interfaces.RefundResult(
                    Success: false,
                    GatewayRefundId: null,
                    ErrorMessage: "Failed to create refund");
            }

            _logger.LogInformation(
                "Stripe refund created: {RefundId} for PaymentIntent {PaymentIntentId}",
                refund.Id,
                request.GatewayTransactionId);

            return new Application.Common.Interfaces.RefundResult(
                Success: true,
                GatewayRefundId: refund.Id);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to process Stripe refund for {TransactionId}", request.GatewayTransactionId);

            return new Application.Common.Interfaces.RefundResult(
                Success: false,
                GatewayRefundId: null,
                ErrorMessage: ex.Message);
        }
    }

    public Task<WebhookValidationResult> ValidateWebhookAsync(
        WebhookPayload payload,
        CancellationToken ct = default)
    {
        try
        {
            var signature = payload.Headers.GetValueOrDefault("Stripe-Signature", "");

            var stripeEvent = _client.ConstructWebhookEvent(
                payload.RawBody,
                signature,
                _webhookSecret);

            if (stripeEvent == null)
            {
                return Task.FromResult(new WebhookValidationResult(
                    IsValid: false,
                    GatewayTransactionId: null,
                    EventType: null,
                    PaymentStatus: null,
                    GatewayEventId: null,
                    ErrorMessage: "Invalid webhook signature"));
            }

            // Extract payment intent from event
            string? paymentIntentId = null;
            PaymentStatus? paymentStatus = null;

            if (stripeEvent.Data.Object is PaymentIntent paymentIntent)
            {
                paymentIntentId = paymentIntent.Id;
                paymentStatus = MapStripeStatusToPaymentStatus(paymentIntent.Status);
            }

            _logger.LogInformation(
                "Stripe webhook validated: {EventType}, PaymentIntent: {PaymentIntentId}",
                stripeEvent.Type,
                paymentIntentId);

            return Task.FromResult(new WebhookValidationResult(
                IsValid: true,
                GatewayTransactionId: paymentIntentId,
                EventType: stripeEvent.Type,
                PaymentStatus: paymentStatus,
                GatewayEventId: stripeEvent.Id));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to validate Stripe webhook");

            return Task.FromResult(new WebhookValidationResult(
                IsValid: false,
                GatewayTransactionId: null,
                EventType: null,
                PaymentStatus: null,
                GatewayEventId: null,
                ErrorMessage: ex.Message));
        }
    }

    public Task<GatewayHealthStatus> HealthCheckAsync(CancellationToken ct = default)
    {
        // Lightweight check: verify credentials are configured
        // Avoid making live API calls (Balance API) to prevent rate limiting
        if (string.IsNullOrEmpty(_secretKey) || string.IsNullOrEmpty(_webhookSecret))
        {
            _logger.LogWarning("Stripe health check failed: Missing credentials");
            return Task.FromResult(GatewayHealthStatus.Unhealthy);
        }

        // Credentials exist - assume healthy (actual API errors will be caught during operations)
        return Task.FromResult(GatewayHealthStatus.Healthy);
    }

    /// <summary>
    /// Maps Stripe payment intent status to internal payment status.
    /// </summary>
    private static PaymentStatus MapStripeStatusToPaymentStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "succeeded" => PaymentStatus.Paid,
            "requires_payment_method" => PaymentStatus.Pending,
            "requires_confirmation" => PaymentStatus.Pending,
            "requires_action" => PaymentStatus.RequiresAction,
            "processing" => PaymentStatus.Processing,
            "canceled" => PaymentStatus.Cancelled,
            "requires_capture" => PaymentStatus.Authorized,
            _ => PaymentStatus.Pending
        };
    }

    /// <summary>
    /// Converts amount to Stripe's smallest currency unit.
    /// </summary>
    private static long GetStripeAmount(decimal amount, string currency)
    {
        // Zero-decimal currencies (no cents)
        var zeroDecimalCurrencies = new[]
        {
            "BIF", "CLP", "DJF", "GNF", "JPY", "KMF", "KRW", "MGA",
            "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"
        };

        if (zeroDecimalCurrencies.Contains(currency.ToUpperInvariant()))
        {
            return (long)amount;
        }

        // Standard currencies - multiply by 100
        return (long)(amount * 100);
    }
}
