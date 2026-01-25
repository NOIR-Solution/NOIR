using Stripe;
using Stripe.Checkout;

namespace NOIR.Infrastructure.Services.Payment.Providers.Stripe;

/// <summary>
/// Stripe API client interface for payment operations.
/// </summary>
public interface IStripeClient
{
    /// <summary>
    /// Initializes the client with a specific API key for multi-tenant scenarios.
    /// </summary>
    void Initialize(string secretKey);

    /// <summary>
    /// Creates a Stripe PaymentIntent.
    /// </summary>
    Task<PaymentIntent> CreatePaymentIntentAsync(
        long amount,
        string currency,
        string? description = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a PaymentIntent by ID.
    /// </summary>
    Task<PaymentIntent?> GetPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    /// <summary>
    /// Creates a refund for a payment.
    /// </summary>
    Task<global::Stripe.Refund?> CreateRefundAsync(
        string paymentIntentId,
        long? amount = null,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels a PaymentIntent.
    /// </summary>
    Task<PaymentIntent?> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);

    /// <summary>
    /// Constructs a webhook event from payload.
    /// </summary>
    Event? ConstructWebhookEvent(string json, string signature, string secret);

    /// <summary>
    /// Creates a Stripe Checkout Session.
    /// </summary>
    Task<Session> CreateCheckoutSessionAsync(
        string successUrl,
        string cancelUrl,
        List<SessionLineItemOptions> lineItems,
        string? customerId = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default);
}

/// <summary>
/// Stripe API client implementation with per-request API key support for multi-tenancy.
/// </summary>
public class StripeClient : IStripeClient
{
    private readonly ILogger<StripeClient> _logger;
    private readonly StripeSettings _settings;
    private string? _apiKey;

    /// <summary>
    /// Gets the RequestOptions for per-request API key (multi-tenant safe).
    /// </summary>
    private RequestOptions RequestOptions => new() { ApiKey = _apiKey ?? _settings.SecretKey };

    public StripeClient(
        IOptions<StripeSettings> settings,
        ILogger<StripeClient> logger)
    {
        _logger = logger;
        _settings = settings.Value;
        // NOTE: Do NOT set StripeConfiguration.ApiKey globally - use per-request RequestOptions instead
    }

    /// <inheritdoc />
    public void Initialize(string secretKey)
    {
        _apiKey = secretKey;
    }

    public async Task<PaymentIntent> CreatePaymentIntentAsync(
        long amount,
        string currency,
        string? description = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = amount,
            Currency = currency.ToLowerInvariant(),
            Description = description,
            Metadata = metadata,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            }
        };

        var service = new PaymentIntentService();
        return await service.CreateAsync(options, RequestOptions, ct);
    }

    public async Task<PaymentIntent?> GetPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            var service = new PaymentIntentService();
            return await service.GetAsync(paymentIntentId, requestOptions: RequestOptions, cancellationToken: ct);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get PaymentIntent {PaymentIntentId}", paymentIntentId);
            return null;
        }
    }

    public async Task<global::Stripe.Refund?> CreateRefundAsync(
        string paymentIntentId,
        long? amount = null,
        string? reason = null,
        CancellationToken ct = default)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId,
                Amount = amount,
                Reason = reason
            };

            var service = new RefundService();
            return await service.CreateAsync(options, RequestOptions, ct);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create refund for PaymentIntent {PaymentIntentId}", paymentIntentId);
            return null;
        }
    }

    public async Task<PaymentIntent?> CancelPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default)
    {
        try
        {
            var service = new PaymentIntentService();
            return await service.CancelAsync(paymentIntentId, options: null, RequestOptions, ct);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to cancel PaymentIntent {PaymentIntentId}", paymentIntentId);
            return null;
        }
    }

    public Event? ConstructWebhookEvent(string json, string signature, string secret)
    {
        try
        {
            return EventUtility.ConstructEvent(json, signature, secret);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to construct Stripe webhook event");
            return null;
        }
    }

    public async Task<Session> CreateCheckoutSessionAsync(
        string successUrl,
        string cancelUrl,
        List<SessionLineItemOptions> lineItems,
        string? customerId = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken ct = default)
    {
        var options = new SessionCreateOptions
        {
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            LineItems = lineItems,
            Mode = "payment",
            Customer = customerId,
            Metadata = metadata
        };

        var service = new SessionService();
        return await service.CreateAsync(options, RequestOptions, ct);
    }
}
