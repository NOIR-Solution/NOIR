namespace NOIR.Application.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Validator for CreateOrderCommand.
/// </summary>
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerEmail)
            .NotEmpty().WithMessage("Customer email is required.")
            .EmailAddress().WithMessage("Customer email must be a valid email address.")
            .MaximumLength(256).WithMessage("Customer email cannot exceed 256 characters.");

        RuleFor(x => x.CustomerName)
            .MaximumLength(200).WithMessage("Customer name cannot exceed 200 characters.");

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(20).WithMessage("Customer phone cannot exceed 20 characters.");

        RuleFor(x => x.ShippingAddress)
            .NotNull().WithMessage("Shipping address is required.")
            .SetValidator(new AddressValidator()!);

        When(x => x.BillingAddress is not null, () =>
        {
            RuleFor(x => x.BillingAddress!)
                .SetValidator(new AddressValidator());
        });

        RuleFor(x => x.ShippingMethod)
            .MaximumLength(100).WithMessage("Shipping method cannot exceed 100 characters.");

        RuleFor(x => x.ShippingAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Shipping amount must be non-negative.");

        RuleFor(x => x.DiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Discount amount must be non-negative.");

        RuleFor(x => x.CouponCode)
            .MaximumLength(50).WithMessage("Coupon code cannot exceed 50 characters.");

        RuleFor(x => x.CustomerNotes)
            .MaximumLength(1000).WithMessage("Customer notes cannot exceed 1000 characters.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .MaximumLength(10).WithMessage("Currency cannot exceed 10 characters.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Order must contain at least one item.")
            .Must(items => items != null && items.Count > 0).WithMessage("Order must contain at least one item.")
            .Must(items => items != null && items.Count <= 100).WithMessage("Order cannot contain more than 100 items.");

        RuleForEach(x => x.Items)
            .SetValidator(new OrderItemValidator());
    }
}

/// <summary>
/// Validator for CreateAddressDto.
/// </summary>
public sealed class AddressValidator : AbstractValidator<CreateAddressDto>
{
    public AddressValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required.")
            .MaximumLength(200).WithMessage("Address line 1 cannot exceed 200 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(200).WithMessage("Address line 2 cannot exceed 200 characters.");

        RuleFor(x => x.Ward)
            .NotEmpty().WithMessage("Ward is required.")
            .MaximumLength(100).WithMessage("Ward cannot exceed 100 characters.");

        RuleFor(x => x.District)
            .NotEmpty().WithMessage("District is required.")
            .MaximumLength(100).WithMessage("District cannot exceed 100 characters.");

        RuleFor(x => x.Province)
            .NotEmpty().WithMessage("Province is required.")
            .MaximumLength(100).WithMessage("Province cannot exceed 100 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code cannot exceed 20 characters.");
    }
}

/// <summary>
/// Validator for CreateOrderItemDto.
/// </summary>
public sealed class OrderItemValidator : AbstractValidator<CreateOrderItemDto>
{
    public OrderItemValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.ProductVariantId)
            .NotEmpty().WithMessage("Product variant ID is required.");

        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.VariantName)
            .NotEmpty().WithMessage("Variant name is required.")
            .MaximumLength(100).WithMessage("Variant name cannot exceed 100 characters.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Unit price must be non-negative.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
            .LessThanOrEqualTo(1000).WithMessage("Quantity cannot exceed 1000.");

        RuleFor(x => x.Sku)
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage("Image URL cannot exceed 500 characters.");

        RuleFor(x => x.OptionsSnapshot)
            .MaximumLength(500).WithMessage("Options snapshot cannot exceed 500 characters.");
    }
}
