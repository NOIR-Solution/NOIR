namespace NOIR.Application.Features.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Role name is required")
            .MinimumLength(2).WithMessage("Role name must be at least 2 characters")
            .MaximumLength(50).WithMessage("Role name cannot exceed 50 characters")
            .Matches("^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Role name must start with a letter and contain only letters, numbers, underscores, and hyphens");
    }
}
