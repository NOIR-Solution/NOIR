namespace NOIR.Application.Features.Checkout.Commands.SelectShippingMethod;

/// <summary>
/// FluentValidation validator for SelectShippingMethodCommand.
/// </summary>
public class SelectShippingMethodCommandValidator : AbstractValidator<SelectShippingMethodCommand>
{
    public SelectShippingMethodCommandValidator()
    {
        RuleFor(x => x.SessionId)
            .NotEmpty()
            .WithMessage("Session ID is required.");

        RuleFor(x => x.ShippingMethod)
            .NotEmpty()
            .WithMessage("Shipping method is required.")
            .MaximumLength(100)
            .WithMessage("Shipping method must not exceed 100 characters.");

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Shipping cost cannot be negative.");
    }
}
