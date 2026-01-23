using NOIR.Application.Features.PlatformSettings.DTOs;

namespace NOIR.Application.Features.PlatformSettings.Commands.UpdateSmtpSettings;

/// <summary>
/// Command to update platform SMTP settings.
/// </summary>
public sealed record UpdateSmtpSettingsCommand(
    string Host,
    int Port,
    string? Username,
    string? Password,
    string FromEmail,
    string FromName,
    bool UseSsl) : IAuditableCommand<SmtpSettingsDto>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string? UserId { get; init; }

    public object? GetTargetId() => "platform-smtp-settings";
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetTargetDisplayName() => "Platform SMTP Settings";
    public string? GetActionDescription() => $"Updated platform SMTP settings (Host: {Host}:{Port})";
}
