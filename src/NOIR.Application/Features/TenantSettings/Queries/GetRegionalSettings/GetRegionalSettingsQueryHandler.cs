using NOIR.Application.Features.TenantSettings.DTOs;

namespace NOIR.Application.Features.TenantSettings.Queries.GetRegionalSettings;

/// <summary>
/// Handler for GetRegionalSettingsQuery.
/// Retrieves regional settings from tenant settings service.
/// </summary>
public class GetRegionalSettingsQueryHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor _tenantAccessor;

    public GetRegionalSettingsQueryHandler(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor tenantAccessor)
    {
        _settingsService = settingsService;
        _tenantAccessor = tenantAccessor;
    }

    public async Task<Result<RegionalSettingsDto>> Handle(
        GetRegionalSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

        var settings = await _settingsService.GetSettingsAsync(tenantId, "regional:", cancellationToken);

        var dto = new RegionalSettingsDto(
            Timezone: settings.GetValueOrDefault("regional:timezone") ?? "UTC",
            Language: settings.GetValueOrDefault("regional:language") ?? "en",
            DateFormat: settings.GetValueOrDefault("regional:date_format") ?? "YYYY-MM-DD");

        return Result.Success(dto);
    }
}
