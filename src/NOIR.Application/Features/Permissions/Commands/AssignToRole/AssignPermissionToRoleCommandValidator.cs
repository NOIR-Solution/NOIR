using DomainPermissions = NOIR.Domain.Common.Permissions;

namespace NOIR.Application.Features.Permissions.Commands.AssignToRole;

public sealed class AssignPermissionToRoleCommandValidator : AbstractValidator<AssignPermissionToRoleCommand>
{
    public AssignPermissionToRoleCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage(localization["validation.roleId.required"]);

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage(localization["validation.permissions.required"]);

        RuleForEach(x => x.Permissions)
            .NotEmpty().WithMessage(localization["validation.permissions.empty"])
            .Must(p => DomainPermissions.All.Contains(p))
            .WithMessage((_, permission) => localization.Get("validation.permissions.invalid", permission));
    }
}
