namespace NOIR.Application.Features.Shipping.Commands.CreateShippingOrder;

/// <summary>
/// Validator for CreateShippingOrderCommand.
/// </summary>
public class CreateShippingOrderCommandValidator : AbstractValidator<CreateShippingOrderCommand>
{
    public CreateShippingOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required.");

        RuleFor(x => x.ProviderCode)
            .IsInEnum()
            .WithMessage("Invalid shipping provider code.");

        RuleFor(x => x.ServiceTypeCode)
            .NotEmpty()
            .WithMessage("Service type code is required.");

        RuleFor(x => x.PickupAddress)
            .NotNull()
            .WithMessage("Pickup address is required.");

        RuleFor(x => x.PickupAddress.FullName)
            .NotEmpty()
            .WithMessage("Pickup address full name is required.")
            .When(x => x.PickupAddress != null);

        RuleFor(x => x.PickupAddress.Phone)
            .NotEmpty()
            .WithMessage("Pickup address phone is required.")
            .When(x => x.PickupAddress != null);

        RuleFor(x => x.DeliveryAddress)
            .NotNull()
            .WithMessage("Delivery address is required.");

        RuleFor(x => x.DeliveryAddress.FullName)
            .NotEmpty()
            .WithMessage("Delivery address full name is required.")
            .When(x => x.DeliveryAddress != null);

        RuleFor(x => x.DeliveryAddress.Phone)
            .NotEmpty()
            .WithMessage("Delivery address phone is required.")
            .When(x => x.DeliveryAddress != null);

        RuleFor(x => x.Sender)
            .NotNull()
            .WithMessage("Sender information is required.");

        RuleFor(x => x.Recipient)
            .NotNull()
            .WithMessage("Recipient information is required.");

        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("At least one item is required.");

        RuleFor(x => x.TotalWeightGrams)
            .GreaterThan(0)
            .WithMessage("Total weight must be greater than 0.");

        RuleFor(x => x.DeclaredValue)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Declared value must be non-negative.");

        RuleFor(x => x.CodAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("COD amount must be non-negative.")
            .When(x => x.CodAmount.HasValue);
    }
}
