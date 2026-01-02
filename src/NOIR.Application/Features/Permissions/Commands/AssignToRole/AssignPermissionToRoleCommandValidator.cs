using DomainPermissions = NOIR.Domain.Common.Permissions;

namespace NOIR.Application.Features.Permissions.Commands.AssignToRole;

public sealed class AssignPermissionToRoleCommandValidator : AbstractValidator<AssignPermissionToRoleCommand>
{
    public AssignPermissionToRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.Permissions)
            .NotNull().WithMessage("Permissions list is required");

        RuleForEach(x => x.Permissions)
            .NotEmpty().WithMessage("Permission cannot be empty")
            .Must(p => DomainPermissions.All.Contains(p))
            .WithMessage("Invalid permission: {PropertyValue}");
    }
}
