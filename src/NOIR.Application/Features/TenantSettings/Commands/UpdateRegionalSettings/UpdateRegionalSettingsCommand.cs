using NOIR.Application.Features.TenantSettings.DTOs;

namespace NOIR.Application.Features.TenantSettings.Commands.UpdateRegionalSettings;

/// <summary>
/// Command to update regional settings for the current tenant.
/// </summary>
public sealed record UpdateRegionalSettingsCommand(
    string Timezone,
    string Language,
    string DateFormat) : IAuditableCommand<RegionalSettingsDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => "Regional Settings";
    public string? GetActionDescription() => "Updated regional settings";
}
