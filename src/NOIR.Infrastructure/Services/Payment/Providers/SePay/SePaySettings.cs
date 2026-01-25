namespace NOIR.Infrastructure.Services.Payment.Providers.SePay;

/// <summary>
/// Configuration settings for SePay payment gateway.
/// SePay uses VietQR for bank transfers with webhook-based confirmation.
/// </summary>
public class SePaySettings
{
    public const string SectionName = "PaymentGateways:SePay";

    /// <summary>
    /// SePay API Base URL.
    /// Sandbox and Production use the same URL.
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://my.sepay.vn/userapi";

    /// <summary>
    /// SePay QR Code generation base URL.
    /// </summary>
    public string QrBaseUrl { get; set; } = "https://qr.sepay.vn/img";

    /// <summary>
    /// API Token from SePay dashboard.
    /// Used for API authentication.
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Bank account number to receive payments.
    /// </summary>
    public string BankAccountNumber { get; set; } = string.Empty;

    /// <summary>
    /// Bank code (e.g., "MB", "VCB", "TCB").
    /// </summary>
    public string BankCode { get; set; } = string.Empty;

    /// <summary>
    /// Optional webhook API key for authentication.
    /// If set, SePay includes this in webhook headers.
    /// </summary>
    public string? WebhookApiKey { get; set; }

    /// <summary>
    /// QR code image template.
    /// Options: "compact", "compact2", "print", "qr_only"
    /// </summary>
    public string QrTemplate { get; set; } = "compact2";

    /// <summary>
    /// Payment expiration time in minutes.
    /// </summary>
    public int PaymentTimeoutMinutes { get; set; } = 30;
}
