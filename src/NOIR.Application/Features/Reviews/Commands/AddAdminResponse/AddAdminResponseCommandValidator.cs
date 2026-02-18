namespace NOIR.Application.Features.Reviews.Commands.AddAdminResponse;

/// <summary>
/// Validator for AddAdminResponseCommand.
/// </summary>
public sealed class AddAdminResponseCommandValidator : AbstractValidator<AddAdminResponseCommand>
{
    private const int MaxResponseLength = 2000;

    public AddAdminResponseCommandValidator()
    {
        RuleFor(x => x.ReviewId)
            .NotEmpty().WithMessage("Review ID is required.");

        RuleFor(x => x.Response)
            .NotEmpty().WithMessage("Response is required.")
            .MaximumLength(MaxResponseLength).WithMessage($"Response cannot exceed {MaxResponseLength} characters.");
    }
}
