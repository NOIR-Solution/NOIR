namespace NOIR.Application.Features.Wishlists.Commands.UpdateWishlistItemPriority;

public sealed class UpdateWishlistItemPriorityCommandValidator : AbstractValidator<UpdateWishlistItemPriorityCommand>
{
    public UpdateWishlistItemPriorityCommandValidator()
    {
        RuleFor(x => x.WishlistItemId)
            .NotEmpty()
            .WithMessage("Wishlist item ID is required");

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Invalid priority value");
    }
}
