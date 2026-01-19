namespace NOIR.Application.Features.Tenants.Commands.CreateTenant;

public sealed class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    private const int MinIdentifierLength = 2;
    private const int MaxIdentifierLength = 100;
    private const int MinNameLength = 2;
    private const int MaxNameLength = 200;
    private const int MaxDomainLength = 500;
    private const int MaxDescriptionLength = 1000;
    private const int MaxNoteLength = 2000;

    public CreateTenantCommandValidator(ILocalizationService localization)
    {
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

        RuleFor(x => x.Domain)
            .MaximumLength(MaxDomainLength).WithMessage(localization.Get("validation.tenantDomain.maxLength", MaxDomainLength))
            .Matches(@"^[a-z0-9]([a-z0-9-]*[a-z0-9])?(\.[a-z0-9]([a-z0-9-]*[a-z0-9])?)*$")
            .WithMessage(localization["validation.tenantDomain.pattern"])
            .When(x => !string.IsNullOrWhiteSpace(x.Domain));

        RuleFor(x => x.Description)
            .MaximumLength(MaxDescriptionLength).WithMessage(localization.Get("validation.tenantDescription.maxLength", MaxDescriptionLength));

        RuleFor(x => x.Note)
            .MaximumLength(MaxNoteLength).WithMessage(localization.Get("validation.tenantNote.maxLength", MaxNoteLength));
    }
}
