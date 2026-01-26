namespace NOIR.Application.Features.Checkout.Commands.InitiateCheckout;

/// <summary>
/// FluentValidation validator for InitiateCheckoutCommand.
/// </summary>
public class InitiateCheckoutCommandValidator : AbstractValidator<InitiateCheckoutCommand>
{
    public InitiateCheckoutCommandValidator()
    {
        RuleFor(x => x.CartId)
            .NotEmpty()
            .WithMessage("Cart ID is required.");

        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .WithMessage("Customer email is required.")
            .EmailAddress()
            .WithMessage("A valid email address is required.")
            .MaximumLength(256)
            .WithMessage("Email must not exceed 256 characters.");

        RuleFor(x => x.CustomerName)
            .MaximumLength(100)
            .WithMessage("Customer name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.CustomerName));

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(20)
            .WithMessage("Customer phone must not exceed 20 characters.")
            .When(x => !string.IsNullOrEmpty(x.CustomerPhone));
    }
}
