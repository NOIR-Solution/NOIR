namespace NOIR.Application.Features.Wishlists.Commands.CreateWishlist;

public sealed class CreateWishlistCommandValidator : AbstractValidator<CreateWishlistCommand>
{
    public CreateWishlistCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Wishlist name is required")
            .MaximumLength(200)
            .WithMessage("Wishlist name cannot exceed 200 characters");
    }
}
