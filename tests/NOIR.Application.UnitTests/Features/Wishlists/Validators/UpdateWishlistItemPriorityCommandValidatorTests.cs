namespace NOIR.Application.UnitTests.Features.Wishlists.Validators;

/// <summary>
/// Unit tests for UpdateWishlistItemPriorityCommandValidator.
/// Tests all validation rules for updating a wishlist item's priority.
/// </summary>
public class UpdateWishlistItemPriorityCommandValidatorTests
{
    private readonly UpdateWishlistItemPriorityCommandValidator _validator = new();

    #region WishlistItemId Validation

    [Fact]
    public async Task Validate_WhenWishlistItemIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateWishlistItemPriorityCommand(
            WishlistItemId: Guid.Empty,
            Priority: WishlistItemPriority.High);

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
        var command = new UpdateWishlistItemPriorityCommand(
            WishlistItemId: Guid.NewGuid(),
            Priority: WishlistItemPriority.High);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WishlistItemId);
    }

    #endregion

    #region Priority Validation

    [Fact]
    public async Task Validate_WhenPriorityIsInvalid_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateWishlistItemPriorityCommand(
            WishlistItemId: Guid.NewGuid(),
            Priority: (WishlistItemPriority)99);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Priority)
            .WithErrorMessage("Invalid priority value");
    }

    [Theory]
    [InlineData(WishlistItemPriority.None)]
    [InlineData(WishlistItemPriority.Low)]
    [InlineData(WishlistItemPriority.Medium)]
    [InlineData(WishlistItemPriority.High)]
    public async Task Validate_WhenPriorityIsValidEnum_ShouldNotHaveError(WishlistItemPriority priority)
    {
        // Arrange
        var command = new UpdateWishlistItemPriorityCommand(
            WishlistItemId: Guid.NewGuid(),
            Priority: priority);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Priority);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateWishlistItemPriorityCommand(
            WishlistItemId: Guid.NewGuid(),
            Priority: WishlistItemPriority.Medium);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
