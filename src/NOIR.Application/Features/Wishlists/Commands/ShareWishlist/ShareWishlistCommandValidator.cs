namespace NOIR.Application.Features.Wishlists.Commands.ShareWishlist;

public sealed class ShareWishlistCommandValidator : AbstractValidator<ShareWishlistCommand>
{
    public ShareWishlistCommandValidator()
    {
        RuleFor(x => x.WishlistId)
            .NotEmpty()
            .WithMessage("Wishlist ID is required");
    }
}
