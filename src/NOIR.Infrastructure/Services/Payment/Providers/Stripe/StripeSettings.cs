using System.ComponentModel.DataAnnotations;

namespace NOIR.Infrastructure.Services.Payment.Providers.Stripe;

/// <summary>
/// Configuration settings for Stripe payment gateway.
/// </summary>
public class StripeSettings
{
    public const string SectionName = "Stripe";

    /// <summary>
    /// Stripe secret API key (sk_test_* or sk_live_*).
    /// </summary>
    [Required]
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Stripe publishable key for frontend (pk_test_* or pk_live_*).
    /// </summary>
    [Required]
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Webhook signing secret (whsec_*).
    /// </summary>
    [Required]
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// Stripe API version to use.
    /// </summary>
    public string ApiVersion { get; set; } = "2024-12-18.acacia";

    /// <summary>
    /// Payment link expiry in minutes.
    /// </summary>
    public int PaymentTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Default currency for payments.
    /// </summary>
    public string DefaultCurrency { get; set; } = "usd";

    /// <summary>
    /// Supported payment method types.
    /// </summary>
    public string[] PaymentMethodTypes { get; set; } = ["card"];
}
