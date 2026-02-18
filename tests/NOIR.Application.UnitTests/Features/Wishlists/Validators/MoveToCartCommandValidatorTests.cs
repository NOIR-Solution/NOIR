namespace NOIR.Application.UnitTests.Features.Wishlists.Validators;

/// <summary>
/// Unit tests for MoveToCartCommandValidator.
/// Tests all validation rules for moving a wishlist item to the cart.
/// </summary>
public class MoveToCartCommandValidatorTests
{
    private readonly MoveToCartCommandValidator _validator = new();

    #region WishlistItemId Validation

    [Fact]
    public async Task Validate_WhenWishlistItemIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new MoveToCartCommand(WishlistItemId: Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WishlistItemId)
            .WithErrorMessage("Wishlist item ID is required");
    }

    [Fact]
    public async Task Validate_WhenWishlistItemIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new MoveToCartCommand(WishlistItemId: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WishlistItemId);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new MoveToCartCommand(WishlistItemId: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
