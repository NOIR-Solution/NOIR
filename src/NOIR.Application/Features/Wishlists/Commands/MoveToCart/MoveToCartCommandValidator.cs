namespace NOIR.Application.Features.Wishlists.Commands.MoveToCart;

public sealed class MoveToCartCommandValidator : AbstractValidator<MoveToCartCommand>
{
    public MoveToCartCommandValidator()
    {
        RuleFor(x => x.WishlistItemId)
            .NotEmpty()
            .WithMessage("Wishlist item ID is required");
    }
}
