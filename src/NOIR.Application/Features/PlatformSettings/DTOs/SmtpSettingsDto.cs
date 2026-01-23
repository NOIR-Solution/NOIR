namespace NOIR.Application.Features.PlatformSettings.DTOs;

/// <summary>
/// DTO for platform SMTP settings.
/// </summary>
public sealed record SmtpSettingsDto
{
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 25;
    public string? Username { get; init; }
    public bool HasPassword { get; init; }
    public string FromEmail { get; init; } = string.Empty;
    public string FromName { get; init; } = string.Empty;
    public bool UseSsl { get; init; }
    public bool IsConfigured { get; init; }
}
