namespace NOIR.Application.Features.Tenants.Commands.UpdateTenant;

public sealed class UpdateTenantCommandValidator : AbstractValidator<UpdateTenantCommand>
{
    private const int MinIdentifierLength = 2;
    private const int MaxIdentifierLength = 100;
    private const int MinNameLength = 2;
    private const int MaxNameLength = 200;

    public UpdateTenantCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(localization["validation.tenantId.required"]);

        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage(localization["validation.tenantIdentifier.required"])
            .MinimumLength(MinIdentifierLength).WithMessage(localization.Get("validation.tenantIdentifier.minLength", MinIdentifierLength))
            .MaximumLength(MaxIdentifierLength).WithMessage(localization.Get("validation.tenantIdentifier.maxLength", MaxIdentifierLength))
            .Matches("^[a-z0-9][a-z0-9-]*[a-z0-9]$|^[a-z0-9]$")
            .WithMessage(localization["validation.tenantIdentifier.pattern"]);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localization["validation.tenantName.required"])
            .MinimumLength(MinNameLength).WithMessage(localization.Get("validation.tenantName.minLength", MinNameLength))
            .MaximumLength(MaxNameLength).WithMessage(localization.Get("validation.tenantName.maxLength", MaxNameLength));
    }
}
