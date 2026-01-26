namespace NOIR.Application.Features.Checkout.Commands.SelectPaymentMethod;

/// <summary>
/// FluentValidation validator for SelectPaymentMethodCommand.
/// </summary>
public class SelectPaymentMethodCommandValidator : AbstractValidator<SelectPaymentMethodCommand>
{
    public SelectPaymentMethodCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("Invalid payment method.");
    }
}
