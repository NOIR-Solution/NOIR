namespace NOIR.Application.Features.Webhooks.Commands.DeleteWebhookSubscription;

/// <summary>
/// Command to soft delete a webhook subscription.
/// </summary>
public sealed record DeleteWebhookSubscriptionCommand(Guid Id) : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Webhook Subscription";
    public string? GetActionDescription() => "Deleted webhook subscription";
}
