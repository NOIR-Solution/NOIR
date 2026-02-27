namespace NOIR.Application.Features.Webhooks.Commands.TestWebhookSubscription;

/// <summary>
/// Command to send a test ping to a webhook subscription URL.
/// </summary>
public sealed record TestWebhookSubscriptionCommand(Guid Id) : IAuditableCommand<WebhookDeliveryLogDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Webhook Subscription";
    public string? GetActionDescription() => "Sent test webhook delivery";
}
