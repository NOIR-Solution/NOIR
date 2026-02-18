namespace NOIR.Application.Features.Wishlists.Commands.DeleteWishlist;

public sealed class DeleteWishlistCommandValidator : AbstractValidator<DeleteWishlistCommand>
{
    public DeleteWishlistCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Wishlist ID is required");
    }
}
