namespace NOIR.Application.Features.TenantSettings.Commands.RevertTenantSmtpSettings;

/// <summary>
/// Command to revert tenant SMTP settings to platform defaults.
/// Deletes all tenant-specific SMTP settings.
/// </summary>
public sealed record RevertTenantSmtpSettingsCommand : IAuditableCommand
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public AuditOperationType OperationType => AuditOperationType.Delete;
    public object? GetTargetId() => null;
    public string? GetTargetDisplayName() => "Tenant SMTP settings";
    public string? GetActionDescription() => "Reverted tenant SMTP settings to platform default";
}
