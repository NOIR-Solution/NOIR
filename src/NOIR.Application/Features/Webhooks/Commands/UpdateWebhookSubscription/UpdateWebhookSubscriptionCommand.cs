namespace NOIR.Application.Features.Webhooks.Commands.UpdateWebhookSubscription;

/// <summary>
/// Command to update an existing webhook subscription.
/// </summary>
public sealed record UpdateWebhookSubscriptionCommand(
    Guid Id,
    string Name,
    string Url,
    string EventPatterns,
    string? Description,
    string? CustomHeaders,
    int MaxRetries = 5,
    int TimeoutSeconds = 30) : IAuditableCommand<WebhookSubscriptionDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated webhook subscription '{Name}'";
}
