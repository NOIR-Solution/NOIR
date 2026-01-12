namespace NOIR.Application.Features.Tenants.Commands.DeleteTenant;

public sealed class DeleteTenantCommandValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(localization["validation.tenantId.required"]);
    }
}
