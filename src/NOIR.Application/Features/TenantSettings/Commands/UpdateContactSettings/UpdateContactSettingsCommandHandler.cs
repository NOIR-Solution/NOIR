using NOIR.Application.Features.TenantSettings.DTOs;

namespace NOIR.Application.Features.TenantSettings.Commands.UpdateContactSettings;

/// <summary>
/// Handler for UpdateContactSettingsCommand.
/// Updates contact settings using tenant settings service.
/// </summary>
public class UpdateContactSettingsCommandHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor _tenantAccessor;
    private readonly ILogger<UpdateContactSettingsCommandHandler> _logger;

    public UpdateContactSettingsCommandHandler(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor tenantAccessor,
        ILogger<UpdateContactSettingsCommandHandler> logger)
    {
        _settingsService = settingsService;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<Result<ContactSettingsDto>> Handle(
        UpdateContactSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

        _logger.LogInformation("Updating contact settings for tenant {TenantId}", tenantId ?? "platform");

        await _settingsService.SetSettingAsync(tenantId, "contact:email", command.Email ?? string.Empty, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "contact:phone", command.Phone ?? string.Empty, "string", cancellationToken);
        await _settingsService.SetSettingAsync(tenantId, "contact:address", command.Address ?? string.Empty, "string", cancellationToken);

        _logger.LogInformation("Contact settings updated for tenant {TenantId}", tenantId ?? "platform");

        var dto = new ContactSettingsDto(
            Email: command.Email,
            Phone: command.Phone,
            Address: command.Address);

        return Result.Success(dto);
    }
}
