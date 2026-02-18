namespace NOIR.Application.UnitTests.Features.Wishlists.Validators;

/// <summary>
/// Unit tests for AddToWishlistCommandValidator.
/// Tests all validation rules for adding a product to a wishlist.
/// </summary>
public class AddToWishlistCommandValidatorTests
{
    private readonly AddToWishlistCommandValidator _validator = new();

    #region ProductId Validation

    [Fact]
    public async Task Validate_WhenProductIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new AddToWishlistCommand(
            WishlistId: null,
            ProductId: Guid.Empty,
            ProductVariantId: null,
            Note: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required");
    }

    [Fact]
    public async Task Validate_WhenProductIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new AddToWishlistCommand(
            WishlistId: null,
            ProductId: Guid.NewGuid(),
            ProductVariantId: null,
            Note: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ProductId);
    }

    #endregion

    #region Note Validation

    [Fact]
    public async Task Validate_WhenNoteExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = new AddToWishlistCommand(
            WishlistId: null,
            ProductId: Guid.NewGuid(),
            ProductVariantId: null,
            Note: new string('A', 501));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Note)
            .WithErrorMessage("Note cannot exceed 500 characters");
    }

    [Fact]
    public async Task Validate_WhenNoteIs500Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new AddToWishlistCommand(
            WishlistId: null,
            ProductId: Guid.NewGuid(),
            ProductVariantId: null,
            Note: new string('A', 500));

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public async Task Validate_WhenNoteIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = new AddToWishlistCommand(
            WishlistId: null,
            ProductId: Guid.NewGuid(),
            ProductVariantId: null,
            Note: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AddToWishlistCommand(
            WishlistId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            ProductVariantId: Guid.NewGuid(),
            Note: "Want this in red");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AddToWishlistCommand(
            WishlistId: null,
            ProductId: Guid.NewGuid(),
            ProductVariantId: null,
            Note: null);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
