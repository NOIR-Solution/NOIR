using NOIR.Application.Features.ProductAttributes.Commands.AddProductAttributeValue;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Validators;

/// <summary>
/// Unit tests for AddProductAttributeValueCommandValidator.
/// Tests all validation rules for adding a product attribute value.
/// </summary>
public class AddProductAttributeValueCommandValidatorTests
{
    private readonly AddProductAttributeValueCommandValidator _validator = new();

    private static AddProductAttributeValueCommand CreateValidCommand() => new(
        AttributeId: Guid.NewGuid(),
        Value: "red",
        DisplayValue: "Red",
        ColorCode: "#FF0000",
        SwatchUrl: null,
        IconUrl: null,
        SortOrder: 0);

    #region Valid Command

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenMinimalCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new AddProductAttributeValueCommand(
            AttributeId: Guid.NewGuid(),
            Value: "blue",
            DisplayValue: "Blue");

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region AttributeId Validation

    [Fact]
    public async Task Validate_WhenAttributeIdIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { AttributeId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AttributeId)
            .WithErrorMessage("Attribute ID is required.");
    }

    #endregion

    #region Value Validation

    [Fact]
    public async Task Validate_WhenValueIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Value = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
            .WithErrorMessage("Value is required.");
    }

    [Fact]
    public async Task Validate_WhenValueExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { Value = new string('a', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
            .WithErrorMessage("Value must not exceed 200 characters.");
    }

    #endregion

    #region DisplayValue Validation

    [Fact]
    public async Task Validate_WhenDisplayValueIsEmpty_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayValue = "" };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayValue)
            .WithErrorMessage("Display value is required.");
    }

    [Fact]
    public async Task Validate_WhenDisplayValueExceeds200Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayValue = new string('A', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayValue)
            .WithErrorMessage("Display value must not exceed 200 characters.");
    }

    #endregion

    #region ColorCode Validation

    [Theory]
    [InlineData("red")]
    [InlineData("FF0000")]
    [InlineData("#GGGGGG")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    public async Task Validate_WhenColorCodeHasInvalidFormat_ShouldHaveError(string colorCode)
    {
        // Arrange
        var command = CreateValidCommand() with { ColorCode = colorCode };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColorCode)
            .WithErrorMessage("Color code must be a valid hex color (e.g., #FF0000 or #F00).");
    }

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#F00")]
    [InlineData("#aabbcc")]
    [InlineData("#AbC")]
    public async Task Validate_WhenColorCodeHasValidFormat_ShouldNotHaveError(string colorCode)
    {
        // Arrange
        var command = CreateValidCommand() with { ColorCode = colorCode };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ColorCode);
    }

    [Fact]
    public async Task Validate_WhenColorCodeIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ColorCode = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ColorCode);
    }

    [Fact]
    public async Task Validate_WhenColorCodeExceeds20Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { ColorCode = new string('A', 21) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColorCode);
    }

    #endregion

    #region SwatchUrl Validation

    [Fact]
    public async Task Validate_WhenSwatchUrlExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SwatchUrl = new string('a', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SwatchUrl)
            .WithErrorMessage("Swatch URL must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenSwatchUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SwatchUrl = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SwatchUrl);
    }

    #endregion

    #region IconUrl Validation

    [Fact]
    public async Task Validate_WhenIconUrlExceeds500Characters_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { IconUrl = new string('a', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IconUrl)
            .WithErrorMessage("Icon URL must not exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WhenIconUrlIsNull_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { IconUrl = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IconUrl);
    }

    #endregion

    #region SortOrder Validation

    [Fact]
    public async Task Validate_WhenSortOrderIsNegative_ShouldHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder)
            .WithErrorMessage("Sort order must be non-negative.");
    }

    [Fact]
    public async Task Validate_WhenSortOrderIsZero_ShouldNotHaveError()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }

    #endregion
}
