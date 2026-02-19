using NOIR.Application.Features.Products.Commands.UpdateProductOption;

namespace NOIR.Application.UnitTests.Features.Products.Commands.UpdateProductOption;

/// <summary>
/// Unit tests for UpdateProductOptionCommandValidator.
/// </summary>
public class UpdateProductOptionCommandValidatorTests
{
    private readonly UpdateProductOptionCommandValidator _validator;

    public UpdateProductOptionCommandValidatorTests()
    {
        _validator = new UpdateProductOptionCommandValidator();
    }

    private static UpdateProductOptionCommand CreateValidCommand() =>
        new(
            ProductId: Guid.NewGuid(),
            OptionId: Guid.NewGuid(),
            Name: "Color",
            DisplayName: "Product Color",
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

    // ===== Name Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyName_ShouldFail(string? name)
    {
        // Arrange
        var command = CreateValidCommand() with { Name = name! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WithNameExceeding50Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 51) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Option name cannot exceed 50 characters.");
    }

    // ===== DisplayName Validation =====

    [Fact]
    public async Task Validate_WithDisplayNameExceeding100Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayName = new string('A', 101) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WithNullDisplayName_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { DisplayName = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DisplayName);
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
