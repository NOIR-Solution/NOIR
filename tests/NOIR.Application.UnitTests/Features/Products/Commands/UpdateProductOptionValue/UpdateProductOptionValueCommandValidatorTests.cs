using NOIR.Application.Features.Products.Commands.UpdateProductOptionValue;

namespace NOIR.Application.UnitTests.Features.Products.Commands.UpdateProductOptionValue;

/// <summary>
/// Unit tests for UpdateProductOptionValueCommandValidator.
/// </summary>
public class UpdateProductOptionValueCommandValidatorTests
{
    private readonly UpdateProductOptionValueCommandValidator _validator;

    public UpdateProductOptionValueCommandValidatorTests()
    {
        _validator = new UpdateProductOptionValueCommandValidator();
    }

    private static UpdateProductOptionValueCommand CreateValidCommand() =>
        new(
            ProductId: Guid.NewGuid(),
            OptionId: Guid.NewGuid(),
            ValueId: Guid.NewGuid(),
            Value: "red",
            DisplayValue: "Red",
            ColorCode: "#FF0000",
            SwatchUrl: "https://example.com/swatch.png",
            SortOrder: 0);

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand();

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== ProductId Validation =====

    [Fact]
    public async Task Validate_WithEmptyProductId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { ProductId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId)
            .WithErrorMessage("Product ID is required.");
    }

    // ===== OptionId Validation =====

    [Fact]
    public async Task Validate_WithEmptyOptionId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { OptionId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OptionId)
            .WithErrorMessage("Option ID is required.");
    }

    // ===== ValueId Validation =====

    [Fact]
    public async Task Validate_WithEmptyValueId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { ValueId = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ValueId)
            .WithErrorMessage("Value ID is required.");
    }

    // ===== Value Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyValue_ShouldFail(string? value)
    {
        // Arrange
        var command = CreateValidCommand() with { Value = value! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value);
    }

    [Fact]
    public async Task Validate_WithValueExceeding50Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Value = new string('A', 51) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Value)
            .WithErrorMessage("Value cannot exceed 50 characters.");
    }

    // ===== DisplayValue Validation =====

    [Fact]
    public async Task Validate_WithDisplayValueExceeding100Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayValue = new string('A', 101) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayValue)
            .WithErrorMessage("Display value cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WithNullDisplayValue_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayValue = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayValue);
    }

    // ===== ColorCode Validation =====

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#00ff00")]
    public async Task Validate_WithValidColorCode_ShouldPass(string colorCode)
    {
        // Arrange
        var command = CreateValidCommand() with { ColorCode = colorCode };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ColorCode);
    }

    [Theory]
    [InlineData("FF0000")]
    [InlineData("#FFF")]
    [InlineData("#GGGGGG")]
    public async Task Validate_WithInvalidColorCode_ShouldFail(string colorCode)
    {
        // Arrange
        var command = CreateValidCommand() with { ColorCode = colorCode };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ColorCode)
            .WithErrorMessage("Color code must be a valid hex color (e.g., #FF0000).");
    }

    [Fact]
    public async Task Validate_WithNullColorCode_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { ColorCode = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ColorCode);
    }

    // ===== SwatchUrl Validation =====

    [Fact]
    public async Task Validate_WithSwatchUrlExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { SwatchUrl = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SwatchUrl)
            .WithErrorMessage("Swatch URL cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WithNullSwatchUrl_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { SwatchUrl = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SwatchUrl);
    }

    // ===== SortOrder Validation =====

    [Fact]
    public async Task Validate_WithNegativeSortOrder_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SortOrder)
            .WithErrorMessage("Sort order must be non-negative.");
    }
}
