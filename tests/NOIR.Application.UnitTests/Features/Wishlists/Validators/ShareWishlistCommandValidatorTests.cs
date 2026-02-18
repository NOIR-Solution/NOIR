namespace NOIR.Application.UnitTests.Features.Wishlists.Validators;

/// <summary>
/// Unit tests for ShareWishlistCommandValidator.
/// Tests all validation rules for sharing a wishlist.
/// </summary>
public class ShareWishlistCommandValidatorTests
{
    private readonly ShareWishlistCommandValidator _validator = new();

    #region WishlistId Validation

    [Fact]
    public async Task Validate_WhenWishlistIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new ShareWishlistCommand(WishlistId: Guid.Empty);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WishlistId)
            .WithErrorMessage("Wishlist ID is required");
    }

    [Fact]
    public async Task Validate_WhenWishlistIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new ShareWishlistCommand(WishlistId: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WishlistId);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ShareWishlistCommand(WishlistId: Guid.NewGuid());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
