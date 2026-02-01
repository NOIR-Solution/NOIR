namespace NOIR.Application.Features.Users.Commands.AssignRoles;

public sealed class AssignRolesToUserCommandValidator : AbstractValidator<AssignRolesToUserCommand>
{
    public AssignRolesToUserCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage(localization["validation.userId.required"]);

        RuleFor(x => x.RoleNames)
            .NotNull().WithMessage(localization["validation.rolesList.required"]);

        RuleForEach(x => x.RoleNames)
            .NotEmpty().WithMessage(localization["validation.roleName.empty"]);
    }
}
