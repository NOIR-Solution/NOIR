
namespace NOIR.Application.Features.TenantSettings.Commands.UpdateBrandingSettings;

/// <summary>
/// Command to update branding settings for the current tenant.
/// </summary>
public sealed record UpdateBrandingSettingsCommand(
    string? LogoUrl,
    string? FaviconUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    bool DarkModeDefault) : IAuditableCommand<BrandingSettingsDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => UserId;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => "Branding Settings";
    public string? GetActionDescription() => "Updated branding settings";
}
