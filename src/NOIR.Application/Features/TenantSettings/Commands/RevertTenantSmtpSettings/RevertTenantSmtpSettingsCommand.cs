namespace NOIR.Application.Features.TenantSettings.Commands.RevertTenantSmtpSettings;

/// <summary>
/// Command to revert tenant SMTP settings to platform defaults.
/// Deletes all tenant-specific SMTP settings.
/// </summary>
public sealed record RevertTenantSmtpSettingsCommand;
