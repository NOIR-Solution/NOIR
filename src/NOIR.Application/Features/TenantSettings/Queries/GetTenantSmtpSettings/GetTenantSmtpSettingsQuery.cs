namespace NOIR.Application.Features.TenantSettings.Queries.GetTenantSmtpSettings;

/// <summary>
/// Query to get tenant SMTP settings.
/// Returns tenant-specific settings if configured, otherwise platform defaults.
/// </summary>
public sealed record GetTenantSmtpSettingsQuery;
