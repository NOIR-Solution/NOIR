namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Processes incoming webhooks from shipping providers.
/// </summary>
public interface IShippingWebhookProcessor
{
    /// <summary>
    /// Process a webhook from a shipping provider.
    /// </summary>
    /// <param name="providerCode">The provider code (e.g., "GHTK", "GHN")</param>
    /// <param name="rawPayload">The raw webhook payload</param>
    /// <param name="signature">Optional signature for verification</param>
    /// <param name="headers">Request headers for additional context</param>
    /// <param name="ct">Cancellation token</param>
    Task<Result> ProcessWebhookAsync(
        string providerCode,
        string rawPayload,
        string? signature,
        Dictionary<string, string>? headers = null,
        CancellationToken ct = default);
}
