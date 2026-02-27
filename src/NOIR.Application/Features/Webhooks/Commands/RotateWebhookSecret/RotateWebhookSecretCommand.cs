namespace NOIR.Application.Features.Webhooks.Commands.RotateWebhookSecret;

/// <summary>
/// Command to rotate the HMAC-SHA256 secret for a webhook subscription.
/// </summary>
public sealed record RotateWebhookSecretCommand(Guid Id) : IAuditableCommand<WebhookSecretDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "Webhook Subscription";
    public string? GetActionDescription() => "Rotated webhook subscription secret";
}
