namespace NOIR.Application.Features.Wishlists.Commands.RemoveFromWishlist;

public sealed class RemoveFromWishlistCommandValidator : AbstractValidator<RemoveFromWishlistCommand>
{
    public RemoveFromWishlistCommandValidator()
    {
        RuleFor(x => x.WishlistItemId)
            .NotEmpty()
            .WithMessage("Wishlist item ID is required");
    }
}
