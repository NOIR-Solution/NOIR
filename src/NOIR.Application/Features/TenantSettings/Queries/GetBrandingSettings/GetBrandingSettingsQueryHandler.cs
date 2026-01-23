
namespace NOIR.Application.Features.TenantSettings.Queries.GetBrandingSettings;

/// <summary>
/// Handler for GetBrandingSettingsQuery.
/// Retrieves branding settings from tenant settings service.
/// </summary>
public class GetBrandingSettingsQueryHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor _tenantAccessor;

    public GetBrandingSettingsQueryHandler(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor tenantAccessor)
    {
        _settingsService = settingsService;
        _tenantAccessor = tenantAccessor;
    }

    public async Task<Result<BrandingSettingsDto>> Handle(
        GetBrandingSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

        var settings = await _settingsService.GetSettingsAsync(tenantId, "branding:", cancellationToken);

        var dto = new BrandingSettingsDto(
            LogoUrl: settings.GetValueOrDefault("branding:logo_url"),
            FaviconUrl: settings.GetValueOrDefault("branding:favicon_url"),
            PrimaryColor: settings.GetValueOrDefault("branding:primary_color"),
            SecondaryColor: settings.GetValueOrDefault("branding:secondary_color"),
            DarkModeDefault: ParseBool(settings.GetValueOrDefault("branding:dark_mode_default")));

        return Result.Success(dto);
    }

    private static bool ParseBool(string? value)
        => bool.TryParse(value, out var result) && result;
}
