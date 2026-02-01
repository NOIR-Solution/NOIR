
namespace NOIR.Application.Features.TenantSettings.Commands.UpdateContactSettings;

/// <summary>
/// Command to update contact settings for the current tenant.
/// </summary>
public sealed record UpdateContactSettingsCommand(
    string? Email,
    string? Phone,
    string? Address) : IAuditableCommand<ContactSettingsDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => "ContactSettings";
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => "Contact Settings";
    public string? GetActionDescription() => "Updated contact settings";
}
