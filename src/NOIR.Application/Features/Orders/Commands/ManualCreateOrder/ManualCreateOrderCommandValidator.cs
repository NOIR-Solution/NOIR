namespace NOIR.Application.Features.Orders.Commands.ManualCreateOrder;

/// <summary>
/// Validator for ManualCreateOrderCommand.
/// </summary>
public sealed class ManualCreateOrderCommandValidator : AbstractValidator<ManualCreateOrderCommand>
{
    public ManualCreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required.")
            .EmailAddress().WithMessage("Customer email must be a valid email address.")
            .MaximumLength(256).WithMessage("Customer email cannot exceed 256 characters.");

        RuleFor(x => x.CustomerName)
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters.");

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(20).WithMessage("Customer phone cannot exceed 20 characters.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.");

        When(x => x.Items is not null, () =>
        {
            RuleFor(x => x.Items)
                .Must(items => items!.Count <= 100).WithMessage("Order cannot contain more than 100 items.");
        });

        RuleForEach(x => x.Items)
            .SetValidator(new ManualOrderItemValidator());

        When(x => x.ShippingAddress is not null, () =>
        {
            RuleFor(x => x.ShippingAddress!)
                .SetValidator(new CreateOrder.AddressValidator());
        });

        When(x => x.BillingAddress is not null, () =>
        {
            RuleFor(x => x.BillingAddress!)
                .SetValidator(new CreateOrder.AddressValidator());
        });

        RuleFor(x => x.ShippingMethod)
            .MaximumLength(100).WithMessage("Shipping method cannot exceed 100 characters.");

        RuleFor(x => x.ShippingAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Shipping amount must be non-negative.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount amount must be non-negative.");

        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Tax amount must be non-negative.");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters.");

        RuleFor(x => x.CustomerNotes)
            .MaximumLength(1000).WithMessage("Customer notes cannot exceed 1000 characters.");

        RuleFor(x => x.InternalNotes)
            .MaximumLength(2000).WithMessage("Internal notes cannot exceed 2000 characters.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .MaximumLength(10).WithMessage("Currency cannot exceed 10 characters.");
    }
}

/// <summary>
/// Validator for ManualOrderItemDto.
/// </summary>
public sealed class ManualOrderItemValidator : AbstractValidator<ManualOrderItemDto>
{
    public ManualOrderItemValidator()
    {
        RuleFor(x => x.ProductVariantId)
            .NotEmpty().WithMessage("Product variant ID is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(1000).WithMessage("Quantity cannot exceed 1000.");

        When(x => x.UnitPrice.HasValue, () =>
        {
            RuleFor(x => x.UnitPrice!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Unit price must be non-negative.");
        });

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Item discount amount must be non-negative.");
    }
}
