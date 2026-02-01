namespace NOIR.Application.Features.Users.Commands.LockUser;

public sealed class LockUserCommandValidator : AbstractValidator<LockUserCommand>
{
    public LockUserCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.TargetUserId)
            .NotEmpty().WithMessage(localization["validation.userId.required"]);
    }
}
