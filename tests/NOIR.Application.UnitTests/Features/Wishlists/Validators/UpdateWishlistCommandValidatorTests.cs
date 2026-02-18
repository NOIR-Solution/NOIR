namespace NOIR.Application.UnitTests.Features.Wishlists.Validators;

/// <summary>
/// Unit tests for UpdateWishlistCommandValidator.
/// Tests all validation rules for updating a wishlist.
/// </summary>
public class UpdateWishlistCommandValidatorTests
{
    private readonly UpdateWishlistCommandValidator _validator = new();

    #region Id Validation

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateWishlistCommand(
            Id: Guid.Empty,
            Name: "Valid Name",
            IsPublic: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("Wishlist ID is required");
    }

    [Fact]
    public async Task Validate_WhenIdIsValid_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateWishlistCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            IsPublic: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    #endregion

    #region Name Validation

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateWishlistCommand(
            Id: Guid.NewGuid(),
            Name: "",
            IsPublic: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Wishlist name is required");
    }

    [Fact]
    public async Task Validate_WhenNameExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = new UpdateWishlistCommand(
            Id: Guid.NewGuid(),
            Name: new string('A', 201),
            IsPublic: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Wishlist name cannot exceed 200 characters");
    }

    [Fact]
    public async Task Validate_WhenNameIs200Characters_ShouldNotHaveError()
    {
        // Arrange
        var command = new UpdateWishlistCommand(
            Id: Guid.NewGuid(),
            Name: new string('A', 200),
            IsPublic: false);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Valid Command Tests

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new UpdateWishlistCommand(
            Id: Guid.NewGuid(),
            Name: "Updated Wishlist Name",
            IsPublic: true);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
