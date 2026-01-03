namespace NOIR.Application.Features.Roles.Commands.DeleteRole;

public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage(localization["validation.roleId.required"]);
    }
}
