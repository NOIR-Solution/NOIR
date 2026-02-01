namespace NOIR.Application.Features.Auth.Commands.RevokeSession;

/// <summary>
/// Validator for RevokeSessionCommand.
/// </summary>
public sealed class RevokeSessionCommandValidator : AbstractValidator<RevokeSessionCommand>
{
    public RevokeSessionCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("Session ID is required.");
    }
}
