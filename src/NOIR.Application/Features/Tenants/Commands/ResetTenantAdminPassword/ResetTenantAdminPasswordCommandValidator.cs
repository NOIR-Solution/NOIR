namespace NOIR.Application.Features.Tenants.Commands.ResetTenantAdminPassword;

/// <summary>
/// Validator for ResetTenantAdminPasswordCommand.
/// </summary>
public class ResetTenantAdminPasswordCommandValidator : AbstractValidator<ResetTenantAdminPasswordCommand>
{
    public ResetTenantAdminPasswordCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage(localization["validation.required"] ?? "Tenant ID is required");

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .WithMessage(localization["validation.required"] ?? "New password is required")
            .MinimumLength(6)
            .WithMessage(localization["validation.password.minLength"] ?? "Password must be at least 6 characters");
    }
}
