namespace NOIR.Application.Features.Wishlists.Commands.AddToWishlist;

public sealed class AddToWishlistCommandValidator : AbstractValidator<AddToWishlistCommand>
{
    public AddToWishlistCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required");

        RuleFor(x => x.Note)
            .MaximumLength(500)
            .WithMessage("Note cannot exceed 500 characters");
    }
}
