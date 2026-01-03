namespace NOIR.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator(ILocalizationService localization)
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage(localization["validation.userId.required"]);
    }
}
