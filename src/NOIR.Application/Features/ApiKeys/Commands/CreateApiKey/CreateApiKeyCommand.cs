using NOIR.Application.Features.ApiKeys.DTOs;

namespace NOIR.Application.Features.ApiKeys.Commands.CreateApiKey;

/// <summary>
/// Command to create a new API key for the current user.
/// </summary>
public sealed record CreateApiKeyCommand(
    string Name,
    string? Description,
    List<string> Permissions,
    DateTimeOffset? ExpiresAt) : IAuditableCommand<ApiKeyCreatedDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => UserId;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Created API key '{Name}'";
}
