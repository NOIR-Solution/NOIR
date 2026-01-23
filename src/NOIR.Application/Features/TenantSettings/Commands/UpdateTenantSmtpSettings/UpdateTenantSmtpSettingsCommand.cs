namespace NOIR.Application.Features.TenantSettings.Commands.UpdateTenantSmtpSettings;

/// <summary>
/// Command to update tenant SMTP settings (Copy-on-Write pattern).
/// Creates tenant-specific settings if none exist.
/// </summary>
public sealed record UpdateTenantSmtpSettingsCommand(
    string Host,
    int Port,
    string? Username,
    string? Password,
    string FromEmail,
    string FromName,
    bool UseSsl)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }
}
