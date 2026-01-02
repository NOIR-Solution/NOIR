using DomainPermissions = NOIR.Domain.Common.Permissions;

namespace NOIR.Application.Features.Roles.Commands.CreateRole;

public sealed class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MinimumLength(2).WithMessage("Role name must be at least 2 characters")
            .MaximumLength(50).WithMessage("Role name cannot exceed 50 characters")
            .Matches("^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Role name must start with a letter and contain only letters, numbers, underscores, and hyphens");

        RuleForEach(x => x.Permissions)
            .NotEmpty().WithMessage("Permission cannot be empty")
            .Must(p => DomainPermissions.All.Contains(p))
            .WithMessage("Invalid permission: {PropertyValue}");
    }
}
