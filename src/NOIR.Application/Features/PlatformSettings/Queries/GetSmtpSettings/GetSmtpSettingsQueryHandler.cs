
namespace NOIR.Application.Features.PlatformSettings.Queries.GetSmtpSettings;

/// <summary>
/// Handler for getting platform SMTP settings from database.
/// Falls back to appsettings.json values if not configured in DB.
/// </summary>
public class GetSmtpSettingsQueryHandler
{
    private readonly ITenantSettingsService _settingsService;

    public GetSmtpSettingsQueryHandler(ITenantSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task<Result<SmtpSettingsDto>> Handle(
        GetSmtpSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetSettingsAsync(
            tenantId: null, // Platform level
            keyPrefix: "smtp:",
            cancellationToken: cancellationToken);

        var isConfigured = settings.Count > 0;

        var dto = new SmtpSettingsDto
        {
            Host = settings.GetValueOrDefault("smtp:host", string.Empty),
            Port = int.TryParse(settings.GetValueOrDefault("smtp:port"), out var port) ? port : 25,
            Username = settings.GetValueOrDefault("smtp:username"),
            HasPassword = !string.IsNullOrEmpty(settings.GetValueOrDefault("smtp:password")),
            FromEmail = settings.GetValueOrDefault("smtp:from_email", string.Empty),
            FromName = settings.GetValueOrDefault("smtp:from_name", string.Empty),
            UseSsl = bool.TryParse(settings.GetValueOrDefault("smtp:use_ssl"), out var ssl) && ssl,
            IsConfigured = isConfigured
        };

        return Result.Success(dto);
    }
}
