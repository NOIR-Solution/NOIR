namespace NOIR.Infrastructure.Services.Payment.Providers.VnPay;

/// <summary>
/// Configuration settings for VNPay payment gateway.
/// These settings are used for default configuration; per-tenant credentials are stored encrypted in the database.
/// </summary>
public class VnPaySettings
{
    public const string SectionName = "VnPay";

    /// <summary>
    /// Terminal code provided by VNPay.
    /// </summary>
    public string TmnCode { get; set; } = string.Empty;

    /// <summary>
    /// Hash secret key for signature generation.
    /// </summary>
    public string HashSecret { get; set; } = string.Empty;

    /// <summary>
    /// VNPay payment gateway URL.
    /// </summary>
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

    /// <summary>
    /// VNPay API URL for queries and refunds.
    /// </summary>
    public string ApiUrl { get; set; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";

    /// <summary>
    /// VNPay API version.
    /// </summary>
    public string Version { get; set; } = "2.1.0";

    /// <summary>
    /// Currency code (VNPay primarily supports VND).
    /// </summary>
    public string CurrencyCode { get; set; } = "VND";

    /// <summary>
    /// Payment timeout in minutes.
    /// </summary>
    public int PaymentTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Locale for payment page (vn or en).
    /// </summary>
    public string Locale { get; set; } = "vn";
}
