namespace NOIR.Application.Features.TenantSettings.DTOs;

/// <summary>
/// DTO for branding settings (logo, colors, dark mode).
/// </summary>
public sealed record BrandingSettingsDto(
    string? LogoUrl,
    string? FaviconUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    bool DarkModeDefault);

/// <summary>
/// Request for updating branding settings.
/// </summary>
public sealed record UpdateBrandingSettingsRequest(
    string? LogoUrl,
    string? FaviconUrl,
    string? PrimaryColor,
    string? SecondaryColor,
    bool DarkModeDefault);

/// <summary>
/// DTO for contact settings (email, phone, address).
/// </summary>
public sealed record ContactSettingsDto(
    string? Email,
    string? Phone,
    string? Address);

/// <summary>
/// Request for updating contact settings.
/// </summary>
public sealed record UpdateContactSettingsRequest(
    string? Email,
    string? Phone,
    string? Address);

/// <summary>
/// DTO for regional settings (timezone, language, date format).
/// </summary>
public sealed record RegionalSettingsDto(
    string Timezone,
    string Language,
    string DateFormat);

/// <summary>
/// Request for updating regional settings.
/// </summary>
public sealed record UpdateRegionalSettingsRequest(
    string Timezone,
    string Language,
    string DateFormat);

/// <summary>
/// DTO for tenant SMTP settings with inheritance information.
/// </summary>
public sealed record TenantSmtpSettingsDto
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 25;
    public string? Username { get; init; }
    public bool HasPassword { get; init; }
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public bool UseSsl { get; init; }
    public bool IsConfigured { get; init; }
    /// <summary>True if using platform defaults (no tenant-specific SMTP settings).</summary>
    public bool IsInherited { get; init; }
}

/// <summary>
/// Request for updating tenant SMTP settings.
/// </summary>
public sealed record UpdateTenantSmtpSettingsRequest(
    string Host,
    int Port,
    string? Username,
    string? Password,
    string FromEmail,
    string FromName,
    bool UseSsl);

/// <summary>
/// Request for testing tenant SMTP connection.
/// </summary>
public sealed record TestTenantSmtpRequest(string RecipientEmail);
