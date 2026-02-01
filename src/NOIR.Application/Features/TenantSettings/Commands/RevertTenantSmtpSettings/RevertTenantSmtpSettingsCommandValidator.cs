namespace NOIR.Application.Features.TenantSettings.Commands.RevertTenantSmtpSettings;

/// <summary>
/// Validator for RevertTenantSmtpSettingsCommand.
/// </summary>
public sealed class RevertTenantSmtpSettingsCommandValidator : AbstractValidator<RevertTenantSmtpSettingsCommand>
{
    public RevertTenantSmtpSettingsCommandValidator()
    {
        // No validation needed - reverts all tenant SMTP settings to platform defaults.
    }
}
