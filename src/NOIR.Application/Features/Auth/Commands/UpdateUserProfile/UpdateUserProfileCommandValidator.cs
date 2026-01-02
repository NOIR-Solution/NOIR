namespace NOIR.Application.Features.Auth.Commands.UpdateUserProfile;

/// <summary>
/// Validator for UpdateUserProfileCommand.
/// Limits harmonized with UpdateUserCommandValidator (50 chars for first/last name).
/// </summary>
public sealed class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(50)
            .When(x => x.FirstName is not null)
            .WithMessage("First name cannot exceed 50 characters");

        RuleFor(x => x.LastName)
            .MaximumLength(50)
            .When(x => x.LastName is not null)
            .WithMessage("Last name cannot exceed 50 characters");
    }
}
