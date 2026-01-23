using NOIR.Application.Features.TenantSettings.DTOs;

namespace NOIR.Application.Features.TenantSettings.Commands.RevertTenantSmtpSettings;

/// <summary>
/// Handler for RevertTenantSmtpSettingsCommand.
/// Deletes tenant-specific SMTP settings and returns platform defaults.
/// </summary>
public class RevertTenantSmtpSettingsCommandHandler
{
    private readonly ITenantSettingsService _settingsService;
    private readonly IMultiTenantContextAccessor _tenantAccessor;
    private readonly ILogger<RevertTenantSmtpSettingsCommandHandler> _logger;

    private static readonly string[] SmtpKeys =
    [
        "smtp:host",
        "smtp:port",
        "smtp:username",
        "smtp:password",
        "smtp:from_email",
        "smtp:from_name",
        "smtp:use_ssl"
    ];

    public RevertTenantSmtpSettingsCommandHandler(
        ITenantSettingsService settingsService,
        IMultiTenantContextAccessor tenantAccessor,
        ILogger<RevertTenantSmtpSettingsCommandHandler> logger)
    {
        _settingsService = settingsService;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    public async Task<Result<TenantSmtpSettingsDto>> Handle(
        RevertTenantSmtpSettingsCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (tenantId is null)
        {
            return Result.Failure<TenantSmtpSettingsDto>(
                Error.Validation("tenantId", "Tenant context is required for this operation."));
        }

        // Delete all tenant-specific SMTP settings
        foreach (var key in SmtpKeys)
        {
            await _settingsService.DeleteSettingAsync(tenantId, key, cancellationToken);
        }

        _logger.LogInformation("Tenant {TenantId} SMTP settings reverted to platform defaults", tenantId);

        // Return platform defaults
        var platformSettings = await _settingsService.GetSettingsAsync(
            tenantId: null,
            keyPrefix: "smtp:",
            cancellationToken: cancellationToken);

        var dto = new TenantSmtpSettingsDto
        {
            Host = platformSettings.GetValueOrDefault("smtp:host", string.Empty),
            Port = int.TryParse(platformSettings.GetValueOrDefault("smtp:port"), out var port) ? port : 25,
            Username = platformSettings.GetValueOrDefault("smtp:username"),
            HasPassword = !string.IsNullOrEmpty(platformSettings.GetValueOrDefault("smtp:password")),
            FromEmail = platformSettings.GetValueOrDefault("smtp:from_email", string.Empty),
            FromName = platformSettings.GetValueOrDefault("smtp:from_name", string.Empty),
            UseSsl = bool.TryParse(platformSettings.GetValueOrDefault("smtp:use_ssl"), out var ssl) && ssl,
            IsConfigured = platformSettings.Count > 0,
            IsInherited = true
        };

        return Result.Success(dto);
    }
}
