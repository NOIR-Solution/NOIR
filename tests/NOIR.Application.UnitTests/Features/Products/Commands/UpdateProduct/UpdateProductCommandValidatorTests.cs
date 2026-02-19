using NOIR.Application.Features.Products.Commands.UpdateProduct;

namespace NOIR.Application.UnitTests.Features.Products.Commands.UpdateProduct;

/// <summary>
/// Unit tests for UpdateProductCommandValidator.
/// </summary>
public class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator;

    public UpdateProductCommandValidatorTests()
    {
        _validator = new UpdateProductCommandValidator();
    }

    private static UpdateProductCommand CreateValidCommand() =>
        new(
            Id: Guid.NewGuid(),
            Name: "Test Product",
            Slug: "test-product",
            ShortDescription: "A short description",
            Description: "A longer description",
            DescriptionHtml: "<p>A longer description</p>",
            BasePrice: 29.99m,
            Currency: "USD",
            CategoryId: Guid.NewGuid(),
            BrandId: Guid.NewGuid(),
            Brand: "TestBrand",
            Sku: "SKU-001",
            Barcode: "1234567890",
            TrackInventory: true,
            MetaTitle: "Test Product",
            MetaDescription: "Meta description",
            SortOrder: 0,
            Weight: 1.5m,
            WeightUnit: "kg",
            Length: 10m,
            Width: 5m,
            Height: 3m,
            DimensionUnit: "cm");

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

    // ===== Id Validation =====

    [Fact]
    public async Task Validate_WithEmptyId_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Id = Guid.Empty };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
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
    public async Task Validate_WithNameExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name cannot exceed 500 characters.");
    }

    [Fact]
    public async Task Validate_WithNameAt500Characters_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Name = new string('A', 500) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    // ===== Slug Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptySlug_ShouldFail(string? slug)
    {
        // Arrange
        var command = CreateValidCommand() with { Slug = slug! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Fact]
    public async Task Validate_WithSlugExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Slug = new string('a', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug);
    }

    [Theory]
    [InlineData("UPPERCASE")]
    [InlineData("has spaces")]
    [InlineData("has_underscore")]
    [InlineData("-starts-with-hyphen")]
    [InlineData("ends-with-hyphen-")]
    [InlineData("double--hyphen")]
    public async Task Validate_WithInvalidSlugFormat_ShouldFail(string slug)
    {
        // Arrange
        var command = CreateValidCommand() with { Slug = slug };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Slug)
            .WithErrorMessage("Slug must be lowercase and contain only letters, numbers, and hyphens.");
    }

    [Theory]
    [InlineData("valid-slug")]
    [InlineData("another-valid-slug-123")]
    [InlineData("simple")]
    public async Task Validate_WithValidSlugFormat_ShouldPass(string slug)
    {
        // Arrange
        var command = CreateValidCommand() with { Slug = slug };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Slug);
    }

    // ===== ShortDescription Validation =====

    [Fact]
    public async Task Validate_WithShortDescriptionExceeding300Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { ShortDescription = new string('A', 301) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShortDescription)
            .WithErrorMessage("Short description cannot exceed 300 characters.");
    }

    [Fact]
    public async Task Validate_WithNullShortDescription_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { ShortDescription = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ShortDescription);
    }

    // ===== Description Validation =====

    [Fact]
    public async Task Validate_WithDescriptionExceeding10000Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Description = new string('A', 10001) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot exceed 10000 characters.");
    }

    [Fact]
    public async Task Validate_WithNullDescription_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Description = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    // ===== BasePrice Validation =====

    [Fact]
    public async Task Validate_WithNegativeBasePrice_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { BasePrice = -1m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BasePrice)
            .WithErrorMessage("Base price must be a non-negative number.");
    }

    [Fact]
    public async Task Validate_WithZeroBasePrice_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { BasePrice = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BasePrice);
    }

    // ===== Currency Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyCurrency_ShouldFail(string? currency)
    {
        // Arrange
        var command = CreateValidCommand() with { Currency = currency! };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    public async Task Validate_WithCurrencyNotExactly3Characters_ShouldFail(string currency)
    {
        // Arrange
        var command = CreateValidCommand() with { Currency = currency };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency code must be 3 characters (ISO 4217).");
    }

    // ===== Brand Validation =====

    [Fact]
    public async Task Validate_WithBrandExceeding200Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Brand = new string('A', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Brand)
            .WithErrorMessage("Brand cannot exceed 200 characters.");
    }

    // ===== Sku Validation =====

    [Fact]
    public async Task Validate_WithSkuExceeding100Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Sku = new string('A', 101) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Sku)
            .WithErrorMessage("SKU cannot exceed 100 characters.");
    }

    // ===== Barcode Validation =====

    [Fact]
    public async Task Validate_WithBarcodeExceeding100Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Barcode = new string('A', 101) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Barcode)
            .WithErrorMessage("Barcode cannot exceed 100 characters.");
    }

    // ===== MetaTitle Validation =====

    [Fact]
    public async Task Validate_WithMetaTitleExceeding200Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { MetaTitle = new string('A', 201) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaTitle)
            .WithErrorMessage("Meta title cannot exceed 200 characters.");
    }

    // ===== MetaDescription Validation =====

    [Fact]
    public async Task Validate_WithMetaDescriptionExceeding500Characters_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { MetaDescription = new string('A', 501) };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MetaDescription)
            .WithErrorMessage("Meta description cannot exceed 500 characters.");
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
            .WithErrorMessage("Sort order must be a non-negative number.");
    }

    // ===== Weight Validation =====

    [Fact]
    public async Task Validate_WithZeroWeight_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Weight = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Weight)
            .WithErrorMessage("Weight must be a positive number.");
    }

    [Fact]
    public async Task Validate_WithNullWeight_ShouldPass()
    {
        // Arrange
        var command = CreateValidCommand() with { Weight = null };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Weight);
    }

    // ===== WeightUnit Validation =====

    [Theory]
    [InlineData("kg")]
    [InlineData("g")]
    [InlineData("lb")]
    [InlineData("oz")]
    public async Task Validate_WithValidWeightUnit_ShouldPass(string unit)
    {
        // Arrange
        var command = CreateValidCommand() with { WeightUnit = unit };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WeightUnit);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("KG")]
    public async Task Validate_WithInvalidWeightUnit_ShouldFail(string unit)
    {
        // Arrange
        var command = CreateValidCommand() with { WeightUnit = unit };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WeightUnit)
            .WithErrorMessage("Weight unit must be one of: kg, g, lb, oz.");
    }

    // ===== Length/Width/Height Validation =====

    [Fact]
    public async Task Validate_WithZeroLength_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Length = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Length)
            .WithErrorMessage("Length must be a positive number.");
    }

    [Fact]
    public async Task Validate_WithZeroWidth_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Width = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Width)
            .WithErrorMessage("Width must be a positive number.");
    }

    [Fact]
    public async Task Validate_WithZeroHeight_ShouldFail()
    {
        // Arrange
        var command = CreateValidCommand() with { Height = 0m };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Height)
            .WithErrorMessage("Height must be a positive number.");
    }

    // ===== DimensionUnit Validation =====

    [Theory]
    [InlineData("cm")]
    [InlineData("in")]
    [InlineData("m")]
    public async Task Validate_WithValidDimensionUnit_ShouldPass(string unit)
    {
        // Arrange
        var command = CreateValidCommand() with { DimensionUnit = unit };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DimensionUnit);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("ft")]
    public async Task Validate_WithInvalidDimensionUnit_ShouldFail(string unit)
    {
        // Arrange
        var command = CreateValidCommand() with { DimensionUnit = unit };

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DimensionUnit)
            .WithErrorMessage("Dimension unit must be one of: cm, in, m.");
    }
}
