namespace NOIR.Application.Common.Settings;

/// <summary>
/// Configuration settings for the payment system.
/// </summary>
public class PaymentSettings
{
    public const string SectionName = "Payment";

    /// <summary>
    /// Prefix for generated transaction numbers.
    /// </summary>
    public string TransactionNumberPrefix { get; set; } = "NOIR";

    /// <summary>
    /// Default currency code.
    /// </summary>
    public string DefaultCurrency { get; set; } = "VND";

    /// <summary>
    /// Base URL for webhook endpoints.
    /// </summary>
    public string WebhookBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Expiry time for idempotency keys in seconds.
    /// </summary>
    public int IdempotencyKeyExpirySeconds { get; set; } = 86400;

    /// <summary>
    /// Payment link expiry time in minutes.
    /// </summary>
    public int PaymentLinkExpiryMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum days after payment within which refunds are allowed.
    /// </summary>
    public int MaxRefundDays { get; set; } = 30;

    /// <summary>
    /// Whether refunds require approval.
    /// </summary>
    public bool RequireRefundApproval { get; set; } = true;

    /// <summary>
    /// Amount threshold above which refund approval is required.
    /// </summary>
    public decimal RefundApprovalThreshold { get; set; } = 1000000;

    /// <summary>
    /// Encryption key ID for payment credentials.
    /// </summary>
    public string EncryptionKeyId { get; set; } = "payment-credentials-key";

    /// <summary>
    /// COD-specific settings.
    /// </summary>
    public CodSettings COD { get; set; } = new();

    /// <summary>
    /// Reconciliation settings.
    /// </summary>
    public ReconciliationSettings Reconciliation { get; set; } = new();
}

/// <summary>
/// Cash on Delivery settings.
/// </summary>
public class CodSettings
{
    /// <summary>
    /// Whether COD is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Maximum amount for COD orders.
    /// </summary>
    public decimal MaxAmount { get; set; } = 10000000;

    /// <summary>
    /// Hours after which to send COD collection reminders.
    /// </summary>
    public int CollectionReminderHours { get; set; } = 48;
}

/// <summary>
/// Payment reconciliation settings.
/// </summary>
public class ReconciliationSettings
{
    /// <summary>
    /// Whether reconciliation is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Cron schedule for reconciliation job.
    /// </summary>
    public string CronSchedule { get; set; } = "0 6 * * *";

    /// <summary>
    /// Email to send reconciliation alerts to.
    /// </summary>
    public string? AlertEmail { get; set; }
}
