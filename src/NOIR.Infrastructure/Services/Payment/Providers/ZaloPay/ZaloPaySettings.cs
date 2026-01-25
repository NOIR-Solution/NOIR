namespace NOIR.Infrastructure.Services.Payment.Providers.ZaloPay;

/// <summary>
/// Configuration settings for ZaloPay payment gateway.
/// </summary>
public class ZaloPaySettings
{
    public const string SectionName = "ZaloPay";

    /// <summary>
    /// Application ID provided by ZaloPay.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Key 1 for MAC signature generation (create order, query).
    /// </summary>
    public string Key1 { get; set; } = string.Empty;

    /// <summary>
    /// Key 2 for callback signature verification.
    /// </summary>
    public string Key2 { get; set; } = string.Empty;

    /// <summary>
    /// ZaloPay API endpoint.
    /// </summary>
    public string Endpoint { get; set; } = "https://sb-openapi.zalopay.vn/v2";

    /// <summary>
    /// Payment timeout in minutes.
    /// </summary>
    public int PaymentTimeoutMinutes { get; set; } = 15;

    /// <summary>
    /// Default bank code (empty for ZaloPay wallet).
    /// </summary>
    public string BankCode { get; set; } = string.Empty;
}
