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
