using DomainPermissions = NOIR.Domain.Common.Permissions;

namespace NOIR.Application.Features.Permissions.Commands.RemoveFromRole;

public sealed class RemovePermissionFromRoleCommandValidator : AbstractValidator<RemovePermissionFromRoleCommand>
{
    public RemovePermissionFromRoleCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage(localization["validation.roleId.required"]);

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage(localization["validation.permissions.required"])
            .NotEmpty().WithMessage(localization["validation.permissions.minOne"]);

        RuleForEach(x => x.Permissions)
            .NotEmpty().WithMessage(localization["validation.permissions.empty"])
            .Must(p => DomainPermissions.All.Contains(p))
            .WithMessage((_, permission) => localization.Get("validation.permissions.invalid", permission));
    }
}
