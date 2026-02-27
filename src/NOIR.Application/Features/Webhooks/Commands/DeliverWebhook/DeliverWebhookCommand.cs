namespace NOIR.Application.Features.Webhooks.Commands.DeliverWebhook;

/// <summary>
/// Command to deliver a webhook payload to a subscription endpoint.
/// This command is published via the message bus and processed asynchronously.
/// On failure, it self-schedules retries with exponential backoff.
/// </summary>
public sealed record DeliverWebhookCommand(
    Guid SubscriptionId,
    Guid DeliveryLogId,
    string EventType,
    Guid EventId,
    string Payload,
    string Secret,
    string Url,
    int TimeoutSeconds,
    int MaxRetries,
    int AttemptNumber,
    string? CustomHeaders);
