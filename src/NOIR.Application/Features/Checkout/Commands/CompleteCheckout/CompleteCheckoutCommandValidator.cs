namespace NOIR.Application.Features.Checkout.Commands.CompleteCheckout;

/// <summary>
/// FluentValidation validator for CompleteCheckoutCommand.
/// </summary>
public class CompleteCheckoutCommandValidator : AbstractValidator<CompleteCheckoutCommand>
{
    public CompleteCheckoutCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required.");

        RuleFor(x => x.CustomerNotes)
            .MaximumLength(2000)
            .WithMessage("Customer notes must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.CustomerNotes));
    }
}
