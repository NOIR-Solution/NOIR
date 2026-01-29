using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProducts;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for GetProductsQueryHandler.
/// Tests product listing with pagination and filtering scenarios.
/// </summary>
public class GetProductsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly GetProductsQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetProductsQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();

        _handler = new GetProductsQueryHandler(
            _productRepositoryMock.Object);
    }

    private static GetProductsQuery CreateTestQuery(
        string? search = null,
        ProductStatus? status = null,
        Guid? categoryId = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStockOnly = null,
        bool? lowStockOnly = null,
        int page = 1,
        int pageSize = 20)
    {
        return new GetProductsQuery(
            search,
            status,
            categoryId,
            brand,
            minPrice,
            maxPrice,
            inStockOnly,
            lowStockOnly,
            page,
            pageSize);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        string slug = "test-product",
        decimal basePrice = 99.99m)
    {
        return Product.Create(name, slug, basePrice, "VND", TestTenantId);
    }

    private static List<Product> CreateTestProducts(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestProduct($"Product {i}", $"product-{i}", 10m * i))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllProducts()
    {
        // Arrange
        var products = CreateTestProducts(5);
        var query = CreateTestQuery();

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var products = CreateTestProducts(10);
        var query = CreateTestQuery(page: 2, pageSize: 5);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.Skip(5).Take(5).ToList());

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(10);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassSearchToSpec()
    {
        // Arrange
        var products = CreateTestProducts(1);
        var query = CreateTestQuery(search: "test");

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldFilterByStatus()
    {
        // Arrange
        var activeProduct = CreateTestProduct("Active Product", "active-product");
        activeProduct.Publish();
        var products = new List<Product> { activeProduct };
        var query = CreateTestQuery(status: ProductStatus.Active);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithBrandFilter_ShouldFilterByBrand()
    {
        // Arrange
        var brandedProduct = CreateTestProduct();
        brandedProduct.SetBrand("TestBrand");
        var products = new List<Product> { brandedProduct };
        var query = CreateTestQuery(brand: "TestBrand");

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithPriceRangeFilter_ShouldFilterByPrice()
    {
        // Arrange
        var products = CreateTestProducts(3);
        var query = CreateTestQuery(minPrice: 10m, maxPrice: 100m);

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoProducts_ShouldReturnEmptyList()
    {
        // Arrange
        var query = CreateTestQuery();

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.HasNextPage.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var query = CreateTestQuery();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsSpec>(), token),
            Times.Once);
        _productRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<ProductsCountSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnPrimaryImageUrl()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddImage("https://example.com/primary.jpg", "Primary", true);
        product.AddImage("https://example.com/secondary.jpg", "Secondary", false);
        var products = new List<Product> { product };
        var query = CreateTestQuery();

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.First().PrimaryImageUrl.Should().Be("https://example.com/primary.jpg");
    }

    [Fact]
    public async Task Handle_WithoutPrimaryImage_ShouldReturnFirstImage()
    {
        // Arrange
        var product = CreateTestProduct();
        product.AddImage("https://example.com/first.jpg", "First", false);
        product.AddImage("https://example.com/second.jpg", "Second", false);
        var products = new List<Product> { product };
        var query = CreateTestQuery();

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.First().PrimaryImageUrl.Should().Be("https://example.com/first.jpg");
    }

    [Fact]
    public async Task Handle_ShouldMapProductListDtoFields()
    {
        // Arrange
        var product = CreateTestProduct("Test Product", "test-product", 99.99m);
        product.SetBrand("Test Brand");
        product.UpdateIdentification("SKU-001", null);
        product.AddVariant("Default", 99.99m); // Add stock
        var products = new List<Product> { product };
        var query = CreateTestQuery();

        _productRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _productRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.First();
        item.Name.Should().Be("Test Product");
        item.Slug.Should().Be("test-product");
        item.BasePrice.Should().Be(99.99m);
        item.Brand.Should().Be("Test Brand");
        item.Sku.Should().Be("SKU-001");
        item.Status.Should().Be(ProductStatus.Draft);
    }

    #endregion
}
