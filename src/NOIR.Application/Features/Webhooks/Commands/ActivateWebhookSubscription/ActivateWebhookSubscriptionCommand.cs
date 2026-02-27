namespace NOIR.Application.Features.Webhooks.Commands.ActivateWebhookSubscription;

/// <summary>
/// Command to activate a webhook subscription.
/// </summary>
public sealed record ActivateWebhookSubscriptionCommand(Guid Id) : IAuditableCommand<WebhookSubscriptionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Webhook Subscription";
    public string? GetActionDescription() => "Activated webhook subscription";
}
