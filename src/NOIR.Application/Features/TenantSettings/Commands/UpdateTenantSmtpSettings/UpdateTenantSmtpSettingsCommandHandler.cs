using NOIR.Application.Features.TenantSettings.DTOs;

namespace NOIR.Application.Features.TenantSettings.Commands.UpdateTenantSmtpSettings;

/// <summary>
/// Handler for UpdateTenantSmtpSettingsCommand.
/// Implements Copy-on-Write: stores settings at tenant level.
/// </summary>
public class UpdateTenantSmtpSettingsCommandHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor _tenantAccessor;
    private readonly ILogger<UpdateTenantSmtpSettingsCommandHandler> _logger;

    public UpdateTenantSmtpSettingsCommandHandler(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor tenantAccessor,
        ILogger<UpdateTenantSmtpSettingsCommandHandler> logger)
    {
        _settingsService = settingsService;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<Result<TenantSmtpSettingsDto>> Handle(
        UpdateTenantSmtpSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (tenantId is null)
        {
            return Result.Failure<TenantSmtpSettingsDto>(
                Error.Validation("tenantId", "Tenant context is required for this operation."));
        }

        // Store all SMTP settings at tenant level (Copy-on-Write)
        await _settingsService.SetSettingAsync(tenantId, "smtp:host", command.Host, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "smtp:port", command.Port.ToString(), "int", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "smtp:from_email", command.FromEmail, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "smtp:from_name", command.FromName, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "smtp:use_ssl", command.UseSsl.ToString().ToLowerInvariant(), "bool", cancellationToken);

        // Only update username if provided
        if (command.Username is not null)
        {
            await _settingsService.SetSettingAsync(tenantId, "smtp:username", command.Username, "string", cancellationToken);
        }

        // Only update password if provided (empty string = clear, null = keep existing)
        if (command.Password is not null)
        {
            await _settingsService.SetSettingAsync(tenantId, "smtp:password", command.Password, "string", cancellationToken);
        }

        _logger.LogInformation("Tenant {TenantId} SMTP settings updated by user {UserId}. Host: {Host}:{Port}",
            tenantId, command.UserId, command.Host, command.Port);

        // Return the updated settings
        var dto = new TenantSmtpSettingsDto
        {
            Host = command.Host,
            Port = command.Port,
            Username = command.Username,
            HasPassword = command.Password is not null
                ? !string.IsNullOrEmpty(command.Password)
                : !string.IsNullOrEmpty(
                    await _settingsService.GetSettingAsync(tenantId, "smtp:password", cancellationToken)),
            FromEmail = command.FromEmail,
            FromName = command.FromName,
            UseSsl = command.UseSsl,
            IsConfigured = true,
            IsInherited = false
        };

        return Result.Success(dto);
    }
}
