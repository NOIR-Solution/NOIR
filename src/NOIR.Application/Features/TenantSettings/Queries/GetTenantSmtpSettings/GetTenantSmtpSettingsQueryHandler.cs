using NOIR.Application.Features.TenantSettings.DTOs;

namespace NOIR.Application.Features.TenantSettings.Queries.GetTenantSmtpSettings;

/// <summary>
/// Handler for GetTenantSmtpSettingsQuery.
/// Retrieves tenant SMTP settings, falling back to platform defaults if not customized.
/// </summary>
public class GetTenantSmtpSettingsQueryHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor _tenantAccessor;

    public GetTenantSmtpSettingsQueryHandler(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor tenantAccessor)
    {
        _settingsService = settingsService;
        _tenantAccessor = tenantAccessor;
    }

    public async Task<Result<TenantSmtpSettingsDto>> Handle(
        GetTenantSmtpSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

        // Check if tenant has any SMTP settings configured
        var hasTenantSettings = tenantId is not null &&
            await _settingsService.SettingExistsAsync(tenantId, "smtp:host", cancellationToken);

        // Get settings from appropriate level
        var settings = await _settingsService.GetSettingsAsync(
            hasTenantSettings ? tenantId : null, // Use tenant if configured, otherwise platform
            keyPrefix: "smtp:",
            cancellationToken: cancellationToken);

        var isConfigured = settings.Count > 0;

        var dto = new TenantSmtpSettingsDto
        {
            Host = settings.GetValueOrDefault("smtp:host", string.Empty),
            Port = int.TryParse(settings.GetValueOrDefault("smtp:port"), out var port) ? port : 25,
            Username = settings.GetValueOrDefault("smtp:username"),
            HasPassword = !string.IsNullOrEmpty(settings.GetValueOrDefault("smtp:password")),
            FromEmail = settings.GetValueOrDefault("smtp:from_email", string.Empty),
            FromName = settings.GetValueOrDefault("smtp:from_name", string.Empty),
            UseSsl = bool.TryParse(settings.GetValueOrDefault("smtp:use_ssl"), out var ssl) && ssl,
            IsConfigured = isConfigured,
            IsInherited = !hasTenantSettings
        };

        return Result.Success(dto);
    }
}
