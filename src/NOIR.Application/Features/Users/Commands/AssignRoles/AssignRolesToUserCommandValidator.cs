namespace NOIR.Application.Features.Users.Commands.AssignRoles;

public sealed class AssignRolesToUserCommandValidator : AbstractValidator<AssignRolesToUserCommand>
{
    public AssignRolesToUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.RoleNames)
            .NotNull().WithMessage("Roles list is required");

        RuleForEach(x => x.RoleNames)
            .NotEmpty().WithMessage("Role name cannot be empty");
    }
}
