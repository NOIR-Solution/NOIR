using NOIR.Application.Features.Products.Commands.BulkImportProducts;

namespace NOIR.Application.UnitTests.Features.Products.Commands.BulkImportProducts;

/// <summary>
/// Unit tests for BulkImportProductsCommandValidator.
/// </summary>
public class BulkImportProductsCommandValidatorTests
{
    private readonly BulkImportProductsCommandValidator _validator;

    public BulkImportProductsCommandValidatorTests()
    {
        _validator = new BulkImportProductsCommandValidator();
    }

    private static ImportProductDto CreateValidImportProduct() =>
        new(
            Name: "Test Product",
            Slug: "test-product",
            BasePrice: 29.99m,
            Currency: "USD",
            ShortDescription: "Short desc",
            Sku: "SKU-001",
            Barcode: "1234567890",
            CategoryName: "Electronics",
            Brand: "TestBrand",
            Stock: 10,
            VariantName: null,
            VariantPrice: null,
            CompareAtPrice: null,
            Images: null,
            Attributes: null);

    private static BulkImportProductsCommand CreateValidCommand() =>
        new(new List<ImportProductDto> { CreateValidImportProduct() });

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

    // ===== Products NotEmpty Validation =====

    [Fact]
    public async Task Validate_WithEmptyProducts_ShouldFail()
    {
        // Arrange
        var command = new BulkImportProductsCommand(new List<ImportProductDto>());

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Products)
            .WithErrorMessage("At least one product is required.");
    }

    // ===== Products Count Validation =====

    [Fact]
    public async Task Validate_WithMoreThan1000Products_ShouldFail()
    {
        // Arrange
        var products = Enumerable.Range(0, 1001)
            .Select(i => CreateValidImportProduct() with { Name = $"Product {i}" })
            .ToList();
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithExactly1000Products_ShouldPass()
    {
        // Arrange
        var products = Enumerable.Range(0, 1000)
            .Select(i => CreateValidImportProduct() with { Name = $"Product {i}" })
            .ToList();
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== Product Name Validation =====

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WithEmptyProductName_ShouldFail(string? name)
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Name = name! }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithProductNameExceeding200Characters_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Name = new string('A', 201) }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    // ===== BasePrice Validation =====

    [Fact]
    public async Task Validate_WithNegativeBasePrice_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { BasePrice = -1m }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithZeroBasePrice_ShouldPass()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { BasePrice = 0m }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== Optional Field Length Validations =====

    [Fact]
    public async Task Validate_WithSlugExceeding200Characters_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Slug = new string('a', 201) }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithCurrencyExceeding3Characters_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Currency = "USDD" }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithShortDescriptionExceeding500Characters_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { ShortDescription = new string('A', 501) }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithSkuExceeding100Characters_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Sku = new string('A', 101) }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithBarcodeExceeding100Characters_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Barcode = new string('A', 101) }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithCategoryNameExceeding100Characters_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { CategoryName = new string('A', 101) }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithBrandExceeding100Characters_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Brand = new string('A', 101) }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    // ===== Numeric Validations =====

    [Fact]
    public async Task Validate_WithNegativeStock_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Stock = -1 }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithNullStock_ShouldPass()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Stock = null }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithVariantNameExceeding100Characters_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { VariantName = new string('A', 101) }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithNegativeVariantPrice_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { VariantPrice = -1m }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithNegativeCompareAtPrice_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { CompareAtPrice = -1m }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    // ===== Images Validation =====

    [Fact]
    public async Task Validate_WithValidPipeSeparatedImages_ShouldPass()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Images = "https://example.com/img1.jpg|https://example.com/img2.jpg" }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithMoreThan10Images_ShouldFail()
    {
        // Arrange
        var images = string.Join("|", Enumerable.Range(0, 11).Select(i => $"https://example.com/img{i}.jpg"));
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Images = images }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithExactly10Images_ShouldPass()
    {
        // Arrange
        var images = string.Join("|", Enumerable.Range(0, 10).Select(i => $"https://example.com/img{i}.jpg"));
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Images = images }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithImageUrlNotStartingWithHttp_ShouldFail()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Images = "ftp://example.com/img.jpg" }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithImageUrlExceeding2000Characters_ShouldFail()
    {
        // Arrange
        var longUrl = "https://example.com/" + new string('a', 1985); // Exceeds 2000 total
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Images = longUrl }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithNullImages_ShouldPass()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Images = null }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WithHttpImageUrl_ShouldPass()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateValidImportProduct() with { Images = "http://example.com/img.jpg" }
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ===== All Optional Fields Null =====

    [Fact]
    public async Task Validate_WithMinimalProduct_ShouldPass()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            new(
                Name: "Minimal Product",
                Slug: null,
                BasePrice: 0m,
                Currency: null,
                ShortDescription: null,
                Sku: null,
                Barcode: null,
                CategoryName: null,
                Brand: null,
                Stock: null,
                VariantName: null,
                VariantPrice: null,
                CompareAtPrice: null,
                Images: null,
                Attributes: null)
        };
        var command = new BulkImportProductsCommand(products);

        // Act
        var result = await _validator.TestValidateAsync(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
