namespace NOIR.Infrastructure.Services.Payment.Providers.MoMo;

/// <summary>
/// Configuration settings for MoMo payment gateway.
/// </summary>
public class MoMoSettings
{
    public const string SectionName = "MoMo";

    /// <summary>
    /// Partner code provided by MoMo.
    /// </summary>
    public string PartnerCode { get; set; } = string.Empty;

    /// <summary>
    /// Access key for API authentication.
    /// </summary>
    public string AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// Secret key for signature generation.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// MoMo API endpoint.
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api";

    /// <summary>
    /// Request type for payment (captureWallet, payWithATM, payWithCC).
    /// </summary>
    public string RequestType { get; set; } = "captureWallet";

    /// <summary>
    /// Payment timeout in minutes.
    /// </summary>
    public int PaymentTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Language for payment page (vi or en).
    /// </summary>
    public string Lang { get; set; } = "vi";
}
