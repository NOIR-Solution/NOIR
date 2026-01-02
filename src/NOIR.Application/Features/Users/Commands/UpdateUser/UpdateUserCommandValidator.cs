namespace NOIR.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100).WithMessage("Display name cannot exceed 100 characters")
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.FirstName)
            .MaximumLength(50).WithMessage("First name cannot exceed 50 characters")
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters")
            .When(x => x.LastName is not null);
    }
}
