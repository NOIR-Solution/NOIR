using NOIR.Application.Features.ApiKeys.DTOs;

namespace NOIR.Application.Features.ApiKeys.Commands.RevokeApiKey;

/// <summary>
/// Command to revoke an API key. Can be done by the key owner or tenant admin.
/// </summary>
public sealed record RevokeApiKeyCommand(Guid Id, string? Reason = null) : IAuditableCommand<ApiKeyDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "API Key";
    public string? GetActionDescription() => $"Revoked API key{(Reason is not null ? $": {Reason}" : "")}";
}
