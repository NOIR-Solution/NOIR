
namespace NOIR.Application.Features.TenantSettings.Commands.UpdateRegionalSettings;

/// <summary>
/// Handler for UpdateRegionalSettingsCommand.
/// Updates regional settings using tenant settings service.
/// </summary>
public class UpdateRegionalSettingsCommandHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor _tenantAccessor;
    private readonly ILogger<UpdateRegionalSettingsCommandHandler> _logger;

    public UpdateRegionalSettingsCommandHandler(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor tenantAccessor,
        ILogger<UpdateRegionalSettingsCommandHandler> logger)
    {
        _settingsService = settingsService;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<Result<RegionalSettingsDto>> Handle(
        UpdateRegionalSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

        _logger.LogInformation("Updating regional settings for tenant {TenantId}", tenantId ?? "platform");

        await _settingsService.SetSettingAsync(tenantId, "regional:timezone", command.Timezone, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "regional:language", command.Language, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "regional:date_format", command.DateFormat, "string", cancellationToken);

        _logger.LogInformation("Regional settings updated for tenant {TenantId}", tenantId ?? "platform");

        var dto = new RegionalSettingsDto(
            Timezone: command.Timezone,
            Language: command.Language,
            DateFormat: command.DateFormat);

        return Result.Success(dto);
    }
}
