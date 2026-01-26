namespace NOIR.Application.Features.Orders.Commands.ShipOrder;

/// <summary>
/// Validator for ShipOrderCommand.
/// </summary>
public sealed class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("Order ID is required.");

        RuleFor(x => x.TrackingNumber)
            .NotEmpty().WithMessage("Tracking number is required.")
            .MaximumLength(100).WithMessage("Tracking number cannot exceed 100 characters.");

        RuleFor(x => x.ShippingCarrier)
            .NotEmpty().WithMessage("Shipping carrier is required.")
            .MaximumLength(100).WithMessage("Shipping carrier cannot exceed 100 characters.");
    }
}
