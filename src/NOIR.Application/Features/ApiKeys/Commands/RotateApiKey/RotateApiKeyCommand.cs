using NOIR.Application.Features.ApiKeys.DTOs;

namespace NOIR.Application.Features.ApiKeys.Commands.RotateApiKey;

/// <summary>
/// Command to rotate an API key's secret.
/// </summary>
public sealed record RotateApiKeyCommand(Guid Id) : IAuditableCommand<ApiKeyRotatedDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;
    public string? GetTargetDisplayName() => "API Key";
    public string? GetActionDescription() => "Rotated API key secret";
}
