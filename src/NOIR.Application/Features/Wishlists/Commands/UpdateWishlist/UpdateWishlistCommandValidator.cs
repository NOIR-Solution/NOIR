namespace NOIR.Application.Features.Wishlists.Commands.UpdateWishlist;

public sealed class UpdateWishlistCommandValidator : AbstractValidator<UpdateWishlistCommand>
{
    public UpdateWishlistCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Wishlist ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Wishlist name is required")
            .MaximumLength(200)
            .WithMessage("Wishlist name cannot exceed 200 characters");
    }
}
