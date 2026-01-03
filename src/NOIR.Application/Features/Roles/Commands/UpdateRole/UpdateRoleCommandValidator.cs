namespace NOIR.Application.Features.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    private const int MinRoleNameLength = 2;
    private const int MaxRoleNameLength = 50;

    public UpdateRoleCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage(localization["validation.roleId.required"]);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(localization["validation.roleName.required"])
            .MinimumLength(MinRoleNameLength).WithMessage(localization.Get("validation.roleName.minLength", MinRoleNameLength))
            .MaximumLength(MaxRoleNameLength).WithMessage(localization.Get("validation.roleName.maxLength", MaxRoleNameLength))
            .Matches("^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage(localization["validation.roleName.pattern"]);
    }
}
