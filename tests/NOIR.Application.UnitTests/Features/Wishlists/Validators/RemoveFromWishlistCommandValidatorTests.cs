namespace NOIR.Application.UnitTests.Features.Wishlists.Validators;

/// <summary>
/// Unit tests for RemoveFromWishlistCommandValidator.
/// Tests all validation rules for removing an item from a wishlist.
/// </summary>
public class RemoveFromWishlistCommandValidatorTests
{
    private readonly RemoveFromWishlistCommandValidator _validator = new();

    #region WishlistItemId Validation

    [Fact]
    public async Task Validate_WhenWishlistItemIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new RemoveFromWishlistCommand(WishlistItemId: Guid.Empty);

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
        var command = new RemoveFromWishlistCommand(WishlistItemId: Guid.NewGuid());

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
        var command = new RemoveFromWishlistCommand(WishlistItemId: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
