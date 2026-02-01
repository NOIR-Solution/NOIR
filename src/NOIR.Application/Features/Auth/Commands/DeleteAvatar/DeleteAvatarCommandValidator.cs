namespace NOIR.Application.Features.Auth.Commands.DeleteAvatar;

/// <summary>
/// Validator for DeleteAvatarCommand.
/// </summary>
public sealed class DeleteAvatarCommandValidator : AbstractValidator<DeleteAvatarCommand>
{
    public DeleteAvatarCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");
    }
}
