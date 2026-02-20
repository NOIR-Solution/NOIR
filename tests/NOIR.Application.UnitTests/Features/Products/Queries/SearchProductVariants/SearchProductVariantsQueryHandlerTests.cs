using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProducts;
using NOIR.Application.Features.Products.Queries.SearchProductVariants;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products.Queries.SearchProductVariants;

/// <summary>
/// Unit tests for SearchProductVariantsQueryHandler.
/// Tests variant search scenarios with mocked product repository.
/// </summary>
public class SearchProductVariantsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly SearchProductVariantsQueryHandler _handler;

    public SearchProductVariantsQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _handler = new SearchProductVariantsQueryHandler(_productRepositoryMock.Object);
    }

    private static Product CreateTestProduct(string name = "Test Product")
    {
        var product = Product.Create(name, name.ToLowerInvariant().Replace(' ', '-'), 100m, "VND", "test-tenant");
        product.Publish();
        return product;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllVariants()
    {
        // Arrange
        var product1 = CreateTestProduct("Laptop Pro");
        product1.AddVariant("16GB Silver", 25000000m, "LAP-001");
        product1.AddVariant("32GB Space Gray", 35000000m, "LAP-002");

        var product2 = CreateTestProduct("Phone X");
        product2.AddVariant("128GB Black", 15000000m, "PHN-001");

        var query = new SearchProductVariantsQuery();

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<SearchProductsWithVariantsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });

        _productRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<SearchProductsWithVariantsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3); // 2 variants from product1 + 1 from product2
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectVariantData()
    {
        // Arrange
        var product = CreateTestProduct("Laptop Pro");
        var variant = product.AddVariant("16GB Silver", 25000000m, "LAP-001");

        var query = new SearchProductVariantsQuery();

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<SearchProductsWithVariantsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        _productRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<SearchProductsWithVariantsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.First();
        item.Id.Should().Be(variant.Id);
        item.ProductId.Should().Be(product.Id);
        item.ProductName.Should().Be("Laptop Pro");
        item.VariantName.Should().Be("16GB Silver");
        item.Sku.Should().Be("LAP-001");
        item.Price.Should().Be(25000000m);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassSearchToSpec()
    {
        // Arrange
        var query = new SearchProductVariantsQuery(Search: "laptop");

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<SearchProductsWithVariantsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _productRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<SearchProductsWithVariantsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();

        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<SearchProductsWithVariantsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEmptyResults_ShouldReturnEmptyPage()
    {
        // Arrange
        var query = new SearchProductVariantsQuery(Search: "nonexistent");

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<SearchProductsWithVariantsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _productRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<SearchProductsWithVariantsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.Page.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithPagination_ShouldPassCorrectSkipAndTake()
    {
        // Arrange
        var query = new SearchProductVariantsQuery(Page: 3, PageSize: 10);

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<SearchProductsWithVariantsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _productRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<SearchProductsWithVariantsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(3);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var query = new SearchProductVariantsQuery();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<SearchProductsWithVariantsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _productRepositoryMock
            .Setup(x => x.CountAsync(It.IsAny<SearchProductsWithVariantsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<SearchProductsWithVariantsSpec>(), token), Times.Once);
        _productRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<SearchProductsWithVariantsCountSpec>(), token), Times.Once);
    }

    #endregion
}
