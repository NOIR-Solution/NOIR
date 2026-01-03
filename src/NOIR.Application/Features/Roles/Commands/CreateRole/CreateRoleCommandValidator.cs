using DomainPermissions = NOIR.Domain.Common.Permissions;

namespace NOIR.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    private const int MinRoleNameLength = 2;
    private const int MaxRoleNameLength = 50;

    public CreateRoleCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localization["validation.roleName.required"])
            .MinimumLength(MinRoleNameLength).WithMessage(localization.Get("validation.roleName.minLength", MinRoleNameLength))
            .MaximumLength(MaxRoleNameLength).WithMessage(localization.Get("validation.roleName.maxLength", MaxRoleNameLength))
            .Matches("^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage(localization["validation.roleName.pattern"]);

        RuleForEach(x => x.Permissions)
            .NotEmpty().WithMessage(localization["validation.permissions.empty"])
            .Must(p => DomainPermissions.All.Contains(p))
            .WithMessage((_, permission) => localization.Get("validation.permissions.invalid", permission));
    }
}
