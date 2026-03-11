using NOIR.Application.Features.ApiKeys.DTOs;

namespace NOIR.Application.Features.ApiKeys.Commands.UpdateApiKey;

/// <summary>
/// Command to update an API key's name, description, and permissions.
/// </summary>
public sealed record UpdateApiKeyCommand(
    Guid Id,
    string Name,
    string? Description,
    List<string> Permissions) : IAuditableCommand<ApiKeyDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => Name;
    public string? GetActionDescription() => $"Updated API key '{Name}'";
}
