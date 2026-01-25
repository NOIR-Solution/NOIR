using System.ComponentModel.DataAnnotations;

namespace NOIR.Infrastructure.Services.Payment.Providers.PayOS;

/// <summary>
/// Configuration settings for PayOS payment gateway.
/// Modern Vietnam gateway with excellent developer experience.
/// </summary>
public class PayOSSettings
{
    public const string SectionName = "PayOS";

    /// <summary>
    /// PayOS Client ID.
    /// </summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// PayOS API Key.
    /// </summary>
    [Required]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// PayOS Checksum Key for signature verification.
    /// </summary>
    [Required]
    public string ChecksumKey { get; set; } = string.Empty;

    /// <summary>
    /// PayOS API base URL.
    /// </summary>
    [Required]
    [Url]
    public string ApiUrl { get; set; } = "https://api-merchant.payos.vn";

    /// <summary>
    /// Payment link expiry in minutes.
    /// </summary>
    public int PaymentTimeoutMinutes { get; set; } = 15;
}
