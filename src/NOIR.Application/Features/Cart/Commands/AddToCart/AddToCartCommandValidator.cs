namespace NOIR.Application.Features.Cart.Commands.AddToCart;

public sealed class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.ProductVariantId)
            .NotEmpty()
            .WithMessage("Product Variant ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(100)
            .WithMessage("Quantity cannot exceed 100");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.UserId) || !string.IsNullOrEmpty(x.SessionId))
            .WithMessage("Either UserId or SessionId must be provided");
    }
}
