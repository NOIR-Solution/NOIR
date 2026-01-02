using DomainPermissions = NOIR.Domain.Common.Permissions;

namespace NOIR.Application.Features.Permissions.Commands.RemoveFromRole;

public sealed class RemovePermissionFromRoleCommandValidator : AbstractValidator<RemovePermissionFromRoleCommand>
{
    public RemovePermissionFromRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage("Permissions list is required")
            .NotEmpty().WithMessage("At least one permission must be specified");

        RuleForEach(x => x.Permissions)
            .NotEmpty().WithMessage("Permission cannot be empty")
            .Must(p => DomainPermissions.All.Contains(p))
            .WithMessage("Invalid permission: {PropertyValue}");
    }
}
