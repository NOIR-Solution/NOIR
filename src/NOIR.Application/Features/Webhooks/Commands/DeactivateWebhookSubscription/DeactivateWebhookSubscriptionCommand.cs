namespace NOIR.Application.Features.Webhooks.Commands.DeactivateWebhookSubscription;

/// <summary>
/// Command to deactivate a webhook subscription.
/// </summary>
public sealed record DeactivateWebhookSubscriptionCommand(Guid Id) : IAuditableCommand<WebhookSubscriptionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Webhook Subscription";
    public string? GetActionDescription() => "Deactivated webhook subscription";
}
