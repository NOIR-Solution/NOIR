using NOIR.Application.Features.Products.Commands.BulkImportProducts;
using NOIR.Application.Features.Products.Specifications;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Commands.BulkImportProducts;

/// <summary>
/// Unit tests for BulkImportProductsCommandHandler.
/// Tests bulk importing products with variants, images, and attributes.
/// </summary>
public class BulkImportProductsCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IInventoryMovementLogger> _movementLoggerMock;
    private readonly Mock<ILogger<BulkImportProductsCommandHandler>> _loggerMock;
    private readonly BulkImportProductsCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public BulkImportProductsCommandHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _movementLoggerMock = new Mock<IInventoryMovementLogger>();
        _loggerMock = new Mock<ILogger<BulkImportProductsCommandHandler>>();

        _handler = new BulkImportProductsCommandHandler(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _attributeRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _movementLoggerMock.Object,
            _loggerMock.Object);
    }

    private static ImportProductDto CreateImportProductDto(
        string name = "Test Product",
        string? slug = null,
        decimal basePrice = 99.99m,
        string? currency = "VND",
        string? shortDescription = null,
        string? sku = null,
        string? barcode = null,
        string? categoryName = null,
        string? brand = null,
        int? stock = null,
        string? variantName = null,
        decimal? variantPrice = null,
        decimal? compareAtPrice = null,
        string? images = null,
        Dictionary<string, string>? attributes = null)
    {
        return new ImportProductDto(
            name,
            slug,
            basePrice,
            currency,
            shortDescription,
            sku,
            barcode,
            categoryName,
            brand,
            stock,
            variantName,
            variantPrice,
            compareAtPrice,
            images,
            attributes);
    }

    private static BulkImportProductsCommand CreateTestCommand(List<ImportProductDto>? products = null)
    {
        return new BulkImportProductsCommand(products ?? [CreateImportProductDto()]);
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category")
    {
        return ProductCategory.Create(name, slug, null, TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithSingleProduct_ShouldImportSuccessfully()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(name: "Product 1", slug: "product-1")
        };
        var command = CreateTestCommand(products);

        SetupEmptyRepositories();

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().Be(1);
        result.Value.Failed.Should().Be(0);
        result.Value.Errors.Should().BeEmpty();

        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleProducts_ShouldImportAll()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(name: "Product 1", slug: "product-1"),
            CreateImportProductDto(name: "Product 2", slug: "product-2"),
            CreateImportProductDto(name: "Product 3", slug: "product-3")
        };
        var command = CreateTestCommand(products);

        SetupEmptyRepositories();

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().Be(3);
        result.Value.Failed.Should().Be(0);

        _productRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_WithCategory_ShouldAssignCategory()
    {
        // Arrange
        var category = CreateTestCategory("Electronics", "electronics");
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(name: "Phone", categoryName: "Electronics")
        };
        var command = CreateTestCommand(products);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([category]);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByCodesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsBySlugsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.CategoryId.Should().Be(category.Id);
    }

    [Fact]
    public async Task Handle_WithVariants_ShouldCreateMultipleVariants()
    {
        // Arrange - Two rows with same slug creates one product with two variants
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(
                name: "T-Shirt",
                slug: "t-shirt",
                variantName: "Small",
                variantPrice: 19.99m,
                stock: 10),
            CreateImportProductDto(
                name: "T-Shirt",
                slug: "t-shirt",
                variantName: "Large",
                variantPrice: 24.99m,
                stock: 5)
        };
        var command = CreateTestCommand(products);

        SetupEmptyRepositories();

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().Be(1); // One product, two variants
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Variants.Should().HaveCount(2);
        capturedProduct.Variants.Should().Contain(v => v.Name == "Small");
        capturedProduct.Variants.Should().Contain(v => v.Name == "Large");
    }

    [Fact]
    public async Task Handle_WithImages_ShouldAddImages()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(
                name: "Product with Images",
                images: "https://example.com/img1.jpg|https://example.com/img2.jpg")
        };
        var command = CreateTestCommand(products);

        SetupEmptyRepositories();

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Images.Should().HaveCount(2);
        capturedProduct.Images.First().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithStock_ShouldLogInventoryMovement()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(name: "Product with Stock", stock: 50)
        };
        var command = CreateTestCommand(products);

        SetupEmptyRepositories();

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _movementLoggerMock.Verify(
            x => x.LogMovementAsync(
                It.IsAny<ProductVariant>(),
                InventoryMovementType.StockIn,
                0,
                50,
                It.IsAny<string?>(),
                It.Is<string?>(s => s != null && s.Contains("Initial stock")),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithBrand_ShouldSetBrand()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(name: "Branded Product", brand: "Apple")
        };
        var command = CreateTestCommand(products);

        SetupEmptyRepositories();

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Brand.Should().Be("Apple");
    }

    #endregion

    #region Slug Handling Scenarios

    [Fact]
    public async Task Handle_WithDuplicateSlug_ShouldGenerateUniqueSlug()
    {
        // Arrange
        var existingProduct = Product.Create("Existing", "product-1", 99.99m, "VND", TestTenantId);
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(name: "New Product", slug: "product-1")
        };
        var command = CreateTestCommand(products);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByCodesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsBySlugsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingProduct]);

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Slug.Should().NotBe("product-1");
        capturedProduct.Slug.Should().StartWith("product-1-");
    }

    [Fact]
    public async Task Handle_WithoutSlug_ShouldGenerateFromName()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(name: "My Awesome Product", slug: null)
        };
        var command = CreateTestCommand(products);

        SetupEmptyRepositories();

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Slug.Should().Be("my-awesome-product");
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task Handle_WithInvalidProduct_ShouldContinueAndReportError()
    {
        // Arrange - Empty name should cause an error
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(name: "", slug: "invalid"), // Invalid
            CreateImportProductDto(name: "Valid Product", slug: "valid") // Valid
        };
        var command = CreateTestCommand(products);

        SetupEmptyRepositories();

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // The empty name product should fail, the valid one should succeed
        result.Value.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithNonExistentCategory_ShouldImportWithoutCategory()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(name: "Product", categoryName: "NonExistent")
        };
        var command = CreateTestCommand(products);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]); // Empty categories

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByCodesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsBySlugsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.CategoryId.Should().BeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithEmptyProductList_ShouldReturnSuccessWithZeroCounts()
    {
        // Arrange
        var command = CreateTestCommand([]);

        SetupEmptyRepositories();

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().Be(0);
        result.Value.Failed.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto()
        };
        var command = CreateTestCommand(products);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        SetupEmptyRepositories();

        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<AllProductCategoriesSpec>(), token),
            Times.Once);
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsBySlugsSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCompareAtPrice_ShouldSetCompareAtPrice()
    {
        // Arrange
        var products = new List<ImportProductDto>
        {
            CreateImportProductDto(
                name: "Sale Product",
                variantPrice: 79.99m,
                compareAtPrice: 99.99m)
        };
        var command = CreateTestCommand(products);

        SetupEmptyRepositories();

        Product? capturedProduct = null;
        _productRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((p, _) => capturedProduct = p)
            .ReturnsAsync((Product p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedProduct.Should().NotBeNull();
        capturedProduct!.Variants.First().CompareAtPrice.Should().Be(99.99m);
    }

    #endregion

    #region Helper Methods

    private void SetupEmptyRepositories()
    {
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByCodesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsBySlugsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    #endregion
}
