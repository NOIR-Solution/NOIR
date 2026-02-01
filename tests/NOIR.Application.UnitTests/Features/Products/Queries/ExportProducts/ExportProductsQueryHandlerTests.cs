using NOIR.Application.Features.Products.Queries.ExportProducts;
using NOIR.Application.Features.Products.Specifications;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Queries.ExportProducts;

/// <summary>
/// Unit tests for ExportProductsQueryHandler.
/// Tests exporting products as flat rows for CSV export.
/// </summary>
public class ExportProductsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly Mock<ILogger<ExportProductsQueryHandler>> _loggerMock;
    private readonly ExportProductsQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ExportProductsQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();
        _loggerMock = new Mock<ILogger<ExportProductsQueryHandler>>();

        _handler = new ExportProductsQueryHandler(
            _productRepositoryMock.Object,
            _categoryRepositoryMock.Object,
            _attributeRepositoryMock.Object,
            _loggerMock.Object);
    }

    private static ExportProductsQuery CreateTestQuery(
        string? categoryId = null,
        string? status = null,
        bool includeAttributes = true,
        bool includeImages = true)
    {
        return new ExportProductsQuery(categoryId, status, includeAttributes, includeImages);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product",
        decimal basePrice = 99.99m)
    {
        return Product.Create(name, slug, basePrice, "VND", TestTenantId);
    }

    private static Product CreateTestProductWithVariants()
    {
        var product = CreateTestProduct();
        product.AddVariant("Small", 19.99m, "SKU-S");
        product.AddVariant("Large", 24.99m, "SKU-L");
        return product;
    }

    private static Product CreateTestProductWithImages()
    {
        var product = CreateTestProduct();
        product.AddImage("https://example.com/img1.jpg", "Image 1", true);
        product.AddImage("https://example.com/img2.jpg", "Image 2", false);
        return product;
    }

    private static ProductCategory CreateTestCategory(
        string name = "Electronics",
        string slug = "electronics")
    {
        return ProductCategory.Create(name, slug, null, TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldExportAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            CreateTestProductWithVariants(),
            CreateTestProduct("Product 2", "product-2")
        };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Rows.Should().HaveCount(3); // 2 variants from first product + 1 from second (no variants = 1 row)
    }

    [Fact]
    public async Task Handle_WithVariants_ShouldCreateRowPerVariant()
    {
        // Arrange
        var product = CreateTestProductWithVariants();
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().HaveCount(2);
        result.Value.Rows.Should().Contain(r => r.VariantName == "Small");
        result.Value.Rows.Should().Contain(r => r.VariantName == "Large");
    }

    [Fact]
    public async Task Handle_WithoutVariants_ShouldCreateSingleRow()
    {
        // Arrange
        var product = CreateTestProduct();
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().HaveCount(1);
        result.Value.Rows.First().VariantName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithCategory_ShouldIncludeCategoryName()
    {
        // Arrange
        var category = CreateTestCategory();
        var product = CreateTestProduct();
        product.SetCategory(category.Id);

        var products = new List<Product> { product };
        var categories = new List<ProductCategory> { category };

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsForExportSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByIdsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = CreateTestQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.First().CategoryName.Should().Be("Electronics");
    }

    [Fact]
    public async Task Handle_WithImages_ShouldIncludePipeSeparatedUrls()
    {
        // Arrange
        var product = CreateTestProductWithImages();
        product.AddVariant("Default", 99.99m, null);
        var products = new List<Product> { product };

        var query = CreateTestQuery(includeImages: true);

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.First().Images.Should().Contain("|");
        result.Value.Rows.First().Images.Should().Contain("img1.jpg");
        result.Value.Rows.First().Images.Should().Contain("img2.jpg");
    }

    [Fact]
    public async Task Handle_WithoutImages_ShouldNotIncludeImages()
    {
        // Arrange
        var product = CreateTestProductWithImages();
        product.AddVariant("Default", 99.99m, null);
        var products = new List<Product> { product };

        var query = CreateTestQuery(includeImages: false);

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.First().Images.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldIncludeVariantPrice()
    {
        // Arrange
        var product = CreateTestProductWithVariants();
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var smallRow = result.Value.Rows.First(r => r.VariantName == "Small");
        smallRow.VariantPrice.Should().Be(19.99m);

        var largeRow = result.Value.Rows.First(r => r.VariantName == "Large");
        largeRow.VariantPrice.Should().Be(24.99m);
    }

    [Fact]
    public async Task Handle_ShouldIncludeVariantStock()
    {
        // Arrange
        var product = CreateTestProductWithVariants();
        foreach (var variant in product.Variants)
        {
            variant.SetStock(10);
        }
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.All(r => r.Stock == 10).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldIncludeVariantSku()
    {
        // Arrange
        var product = CreateTestProductWithVariants();
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().Contain(r => r.Sku == "SKU-S");
        result.Value.Rows.Should().Contain(r => r.Sku == "SKU-L");
    }

    [Fact]
    public async Task Handle_WithCompareAtPrice_ShouldIncludeCompareAtPrice()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = product.AddVariant("Default", 79.99m, null);
        variant.SetCompareAtPrice(99.99m);
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.First().CompareAtPrice.Should().Be(99.99m);
    }

    #endregion

    #region Filter Scenarios

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassToSpec()
    {
        // Arrange
        var query = CreateTestQuery(status: "Active");

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsForExportSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsForExportSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_ShouldPassToSpec()
    {
        // Arrange
        var categoryId = Guid.NewGuid().ToString();
        var query = CreateTestQuery(categoryId: categoryId);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsForExportSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsForExportSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Empty Scenarios

    [Fact]
    public async Task Handle_WithNoProducts_ShouldReturnEmptyRows()
    {
        // Arrange
        var query = CreateTestQuery();

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsForExportSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.Should().BeEmpty();
        result.Value.AttributeColumns.Should().BeEmpty();
    }

    #endregion

    #region Attribute Scenarios

    [Fact]
    public async Task Handle_WithoutAttributesFlag_ShouldNotQueryAttributes()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddVariant("Default", 99.99m, null);
        var products = new List<Product> { product };

        var query = CreateTestQuery(includeAttributes: false);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsForExportSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _attributeRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductAttributesByIdsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithAttributesFlag_ShouldReturnAttributeColumns()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddVariant("Default", 99.99m, null);
        var products = new List<Product> { product };

        var query = CreateTestQuery(includeAttributes: true);

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AttributeColumns.Should().NotBeNull();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var query = CreateTestQuery();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsForExportSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsForExportSpec>(), token),
            Times.Once);
        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<AllProductCategoriesSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMixedProducts_ShouldHandleVariantsCorrectly()
    {
        // Arrange - Mix of products with and without variants
        var productWithVariants = CreateTestProductWithVariants();
        var productWithoutVariants = CreateTestProduct("No Variants", "no-variants");

        var products = new List<Product> { productWithVariants, productWithoutVariants };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // 2 variants from first + 1 row from second (no variants)
        result.Value.Rows.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldIncludeProductStatus()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddVariant("Default", 99.99m, null);
        product.Publish(); // Set to Active
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.First().Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_ShouldIncludeBrand()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddVariant("Default", 99.99m, null);
        product.SetBrand("Apple");
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.First().Brand.Should().Be("Apple");
    }

    [Fact]
    public async Task Handle_ShouldIncludeBarcode()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddVariant("Default", 99.99m, null);
        product.UpdateIdentification("SKU-001", "1234567890123");
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.First().Barcode.Should().Be("1234567890123");
    }

    [Fact]
    public async Task Handle_ShouldIncludeShortDescription()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddVariant("Default", 99.99m, null);
        product.UpdateBasicInfo(product.Name, product.Slug, "Short description", null, null);
        var products = new List<Product> { product };

        var query = CreateTestQuery();

        SetupEmptyDependencies(products);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rows.First().ShortDescription.Should().Be("Short description");
    }

    #endregion

    #region Helper Methods

    private void SetupEmptyDependencies(List<Product> products)
    {
        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsForExportSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByIdsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    #endregion
}
