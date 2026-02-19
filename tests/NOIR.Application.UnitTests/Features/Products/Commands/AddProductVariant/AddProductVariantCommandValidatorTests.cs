using NOIR.Application.Features.Products.Commands.AddProductVariant;

namespace NOIR.Application.UnitTests.Features.Products.Commands.AddProductVariant;

/// <summary>
/// Unit tests for AddProductVariantCommandValidator.
/// </summary>
public class AddProductVariantCommandValidatorTests
{
    private readonly AddProductVariantCommandValidator _validator;

    public AddProductVariantCommandValidatorTests()
    {
        _validator = new AddProductVariantCommandValidator();
    }

    private static AddProductVariantCommand CreateValidCommand() =>
        new(
            ProductId: Guid.NewGuid(),
            Name: "Small",
            Price: 19.99m,
            Sku: "SKU-SM",
            CompareAtPrice: null,
            CostPrice: null,
            StockQuantity: 10,
            Options: null,
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
    public async Task Validate_WithNameExceeding100Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 101) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Variant name cannot exceed 100 characters.");
    }

    [Fact]
    public async Task Validate_WithNameAt100Characters_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 100) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ===== Price Validation =====

    [Fact]
    public async Task Validate_WithNegativePrice_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Price = -1m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Price)
            .WithErrorMessage("Price must be non-negative.");
    }

    [Fact]
    public async Task Validate_WithZeroPrice_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Price = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Price);
    }

    // ===== Sku Validation =====

    [Fact]
    public async Task Validate_WithSkuExceeding50Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Sku = new string('A', 51) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Sku)
            .WithErrorMessage("SKU cannot exceed 50 characters.");
    }

    [Fact]
    public async Task Validate_WithNullSku_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Sku = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Sku);
    }

    // ===== CompareAtPrice Validation =====

    [Fact]
    public async Task Validate_WithCompareAtPriceZero_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { CompareAtPrice = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CompareAtPrice)
            .WithErrorMessage("Compare-at price must be positive.");
    }

    [Fact]
    public async Task Validate_WithCompareAtPriceLessThanPrice_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Price = 29.99m, CompareAtPrice = 19.99m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CompareAtPrice)
            .WithErrorMessage("Compare-at price must be higher than the regular price.");
    }

    [Fact]
    public async Task Validate_WithCompareAtPriceEqualToPrice_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Price = 29.99m, CompareAtPrice = 29.99m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CompareAtPrice);
    }

    [Fact]
    public async Task Validate_WithCompareAtPriceHigherThanPrice_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Price = 19.99m, CompareAtPrice = 29.99m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CompareAtPrice);
    }

    [Fact]
    public async Task Validate_WithNullCompareAtPrice_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { CompareAtPrice = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CompareAtPrice);
    }

    // ===== CostPrice Validation =====

    [Fact]
    public async Task Validate_WithNegativeCostPrice_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { CostPrice = -1m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CostPrice)
            .WithErrorMessage("Cost price must be non-negative.");
    }

    [Fact]
    public async Task Validate_WithZeroCostPrice_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { CostPrice = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostPrice);
    }

    [Fact]
    public async Task Validate_WithNullCostPrice_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { CostPrice = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CostPrice);
    }

    // ===== StockQuantity Validation =====

    [Fact]
    public async Task Validate_WithNegativeStockQuantity_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { StockQuantity = -1 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StockQuantity)
            .WithErrorMessage("Stock quantity must be non-negative.");
    }

    [Fact]
    public async Task Validate_WithZeroStockQuantity_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { StockQuantity = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.StockQuantity);
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

    [Fact]
    public async Task Validate_WithZeroSortOrder_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { SortOrder = 0 };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SortOrder);
    }
}
