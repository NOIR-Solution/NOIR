using NOIR.Application.Features.PlatformSettings.DTOs;

namespace NOIR.Application.Features.PlatformSettings.Commands.UpdateSmtpSettings;

/// <summary>
/// Handler for updating platform SMTP settings.
/// Stores settings in the TenantSettings table with tenantId = null (platform level).
/// </summary>
public class UpdateSmtpSettingsCommandHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly ILogger<UpdateSmtpSettingsCommandHandler> _logger;

    public UpdateSmtpSettingsCommandHandler(
        ITenantSettingsService settingsService,
        ILogger<UpdateSmtpSettingsCommandHandler> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<Result<SmtpSettingsDto>> Handle(
        UpdateSmtpSettingsCommand command,
        CancellationToken cancellationToken)
    {
        // Store all SMTP settings at platform level (tenantId = null)
        await _settingsService.SetSettingAsync(null, "smtp:host", command.Host, "string", cancellationToken);
        await _settingsService.SetSettingAsync(null, "smtp:port", command.Port.ToString(), "int", cancellationToken);
        await _settingsService.SetSettingAsync(null, "smtp:from_email", command.FromEmail, "string", cancellationToken);
        await _settingsService.SetSettingAsync(null, "smtp:from_name", command.FromName, "string", cancellationToken);
        await _settingsService.SetSettingAsync(null, "smtp:use_ssl", command.UseSsl.ToString().ToLowerInvariant(), "bool", cancellationToken);

        // Only update username if provided
        if (command.Username is not null)
        {
            await _settingsService.SetSettingAsync(null, "smtp:username", command.Username, "string", cancellationToken);
        }

        // Only update password if provided (empty string = clear, null = keep existing)
        if (command.Password is not null)
        {
            await _settingsService.SetSettingAsync(null, "smtp:password", command.Password, "string", cancellationToken);
        }

        _logger.LogInformation("Platform SMTP settings updated by user {UserId}. Host: {Host}:{Port}",
            command.UserId, command.Host, command.Port);

        // Return the updated settings
        var dto = new SmtpSettingsDto
        {
            Host = command.Host,
            Port = command.Port,
            Username = command.Username,
            HasPassword = command.Password is not null
                ? !string.IsNullOrEmpty(command.Password)
                : !string.IsNullOrEmpty(
                    await _settingsService.GetSettingAsync(null, "smtp:password", cancellationToken)),
            FromEmail = command.FromEmail,
            FromName = command.FromName,
            UseSsl = command.UseSsl,
            IsConfigured = true
        };

        return Result.Success(dto);
    }
}
