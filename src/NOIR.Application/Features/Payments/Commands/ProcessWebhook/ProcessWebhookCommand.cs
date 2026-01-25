namespace NOIR.Application.Features.Payments.Commands.ProcessWebhook;

/// <summary>
/// Command to process a payment gateway webhook.
/// This is triggered by the webhook endpoint, not directly by users.
/// </summary>
public sealed record ProcessWebhookCommand(
    string Provider,
    string RawPayload,
    string? Signature,
    string? IpAddress,
    Dictionary<string, string>? Headers);
