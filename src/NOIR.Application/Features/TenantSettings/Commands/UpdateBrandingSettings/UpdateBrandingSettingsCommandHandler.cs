
namespace NOIR.Application.Features.TenantSettings.Commands.UpdateBrandingSettings;

/// <summary>
/// Handler for UpdateBrandingSettingsCommand.
/// Updates branding settings using tenant settings service.
/// </summary>
public class UpdateBrandingSettingsCommandHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor _tenantAccessor;
    private readonly ILogger<UpdateBrandingSettingsCommandHandler> _logger;

    public UpdateBrandingSettingsCommandHandler(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor tenantAccessor,
        ILogger<UpdateBrandingSettingsCommandHandler> logger)
    {
        _settingsService = settingsService;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<Result<BrandingSettingsDto>> Handle(
        UpdateBrandingSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

        _logger.LogInformation("Updating branding settings for tenant {TenantId}", tenantId ?? "platform");

        // Update each setting
        await _settingsService.SetSettingAsync(tenantId, "branding:logo_url", command.LogoUrl ?? string.Empty, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "branding:favicon_url", command.FaviconUrl ?? string.Empty, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "branding:primary_color", command.PrimaryColor ?? string.Empty, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "branding:secondary_color", command.SecondaryColor ?? string.Empty, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "branding:dark_mode_default", command.DarkModeDefault.ToString().ToLower(), "bool", cancellationToken);

        _logger.LogInformation("Branding settings updated for tenant {TenantId}", tenantId ?? "platform");

        var dto = new BrandingSettingsDto(
            LogoUrl: command.LogoUrl,
            FaviconUrl: command.FaviconUrl,
            PrimaryColor: command.PrimaryColor,
            SecondaryColor: command.SecondaryColor,
            DarkModeDefault: command.DarkModeDefault);

        return Result.Success(dto);
    }
}
