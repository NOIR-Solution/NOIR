using NOIR.Application.Features.TenantSettings.DTOs;

namespace NOIR.Application.Features.TenantSettings.Queries.GetContactSettings;

/// <summary>
/// Handler for GetContactSettingsQuery.
/// Retrieves contact settings from tenant settings service.
/// </summary>
public class GetContactSettingsQueryHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor _tenantAccessor;

    public GetContactSettingsQueryHandler(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor tenantAccessor)
    {
        _settingsService = settingsService;
        _tenantAccessor = tenantAccessor;
    }

    public async Task<Result<ContactSettingsDto>> Handle(
        GetContactSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

        var settings = await _settingsService.GetSettingsAsync(tenantId, "contact:", cancellationToken);

        var dto = new ContactSettingsDto(
            Email: settings.GetValueOrDefault("contact:email"),
            Phone: settings.GetValueOrDefault("contact:phone"),
            Address: settings.GetValueOrDefault("contact:address"));

        return Result.Success(dto);
    }
}
