namespace NOIR.Application.Features.Shipping.Commands.CancelShippingOrder;

/// <summary>
/// Validator for CancelShippingOrderCommand.
/// </summary>
public class CancelShippingOrderCommandValidator : AbstractValidator<CancelShippingOrderCommand>
{
    public CancelShippingOrderCommandValidator()
    {
        RuleFor(x => x.TrackingNumber)
            .NotEmpty()
            .WithMessage("Tracking number is required.");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Reason must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
