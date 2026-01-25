namespace NOIR.Domain.Enums;

/// <summary>
/// Status of webhook processing.
/// </summary>
public enum WebhookProcessingStatus
{
    /// <summary>
    /// Webhook received but not yet processed.
    /// </summary>
    Received = 0,

    /// <summary>
    /// Webhook is currently being processed.
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Webhook successfully processed.
    /// </summary>
    Processed = 2,

    /// <summary>
    /// Webhook processing failed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Webhook skipped (duplicate or irrelevant).
    /// </summary>
    Skipped = 4
}
