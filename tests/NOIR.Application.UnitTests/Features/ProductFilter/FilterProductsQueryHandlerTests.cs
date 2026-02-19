using NOIR.Application.Features.ProductFilter.DTOs;
using NOIR.Application.Features.ProductFilter.Queries.FilterProducts;
using NOIR.Application.Features.ProductFilter.Services;
using ProductFilterIndexEntity = NOIR.Domain.Entities.Product.ProductFilterIndex;

namespace NOIR.Application.UnitTests.Features.ProductFilter;

/// <summary>
/// Unit tests for FilterProductsQueryHandler.
/// Tests product filtering with various filter combinations, sorting, and pagination.
/// Due to complex IQueryable chain operations with GroupBy/Select/Include,
/// these tests focus on verifiable paths and mocked DbSet behavior.
/// </summary>
public class FilterProductsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly FacetCalculator _facetCalculator;
    private readonly Mock<ILogger<FilterProductsQueryHandler>> _loggerMock;
    private readonly FilterProductsQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public FilterProductsQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _loggerMock = new Mock<ILogger<FilterProductsQueryHandler>>();

        // FacetCalculator is a concrete class with non-virtual methods, so we use a real instance
        // backed by the same mock dbContext
        var facetLoggerMock = new Mock<ILogger<FacetCalculator>>();
        _facetCalculator = new FacetCalculator(
            _dbContextMock.Object,
            facetLoggerMock.Object);

        _handler = new FilterProductsQueryHandler(
            _dbContextMock.Object,
            _facetCalculator,
            _loggerMock.Object);
    }

    private static ProductFilterIndexEntity CreateTestFilterIndex(
        Guid? productId = null,
        string name = "Test Product",
        string slug = "test-product",
        ProductStatus status = ProductStatus.Active,
        decimal minPrice = 100m,
        decimal maxPrice = 200m,
        string currency = "VND",
        bool inStock = true,
        int totalStock = 10)
    {
        var index = ProductFilterIndexEntity.Create(
            productId ?? Guid.NewGuid(),
            name,
            slug,
            status,
            minPrice,
            currency,
            TestTenantId);

        // Use UpdatePricing to set correct max price
        index.UpdatePricing(minPrice, maxPrice, currency);
        index.UpdateStock(totalStock, inStock);

        return index;
    }

    private void SetupProductFilterIndexes(List<ProductFilterIndexEntity> indexes)
    {
        var mockDbSet = indexes.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductFilterIndexes).Returns(mockDbSet.Object);
    }

    private void SetupProductCategories(List<ProductCategory> categories)
    {
        var mockDbSet = categories.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductCategories).Returns(mockDbSet.Object);
    }

    private void SetupFacetCalculatorDependencies()
    {
        // FacetCalculator uses the same _dbContextMock so it reads from ProductAttributes DbSet
        // We need to set up ProductAttributes for the FacetCalculator to query
        var emptyAttributes = new List<ProductAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributes).Returns(emptyAttributes.Object);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Assert
        _handler.Should().NotBeNull();
    }

    #endregion

    #region Default Query (No Filters)

    [Fact]
    public async Task Handle_WithNoFiltersAndEmptyData_ShouldReturnSuccessWithEmptyProducts()
    {
        // Arrange
        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>());
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(24);
    }

    [Fact]
    public async Task Handle_WithActiveProducts_ShouldReturnFilteredResults()
    {
        // Arrange
        var product1 = CreateTestFilterIndex(name: "Product A", slug: "product-a");
        var product2 = CreateTestFilterIndex(name: "Product B", slug: "product-b");

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { product1, product2 });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(2);
        result.Value.Total.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnActiveProducts()
    {
        // Arrange
        var activeProduct = CreateTestFilterIndex(
            name: "Active Product", slug: "active-product", status: ProductStatus.Active);
        var draftProduct = CreateTestFilterIndex(
            name: "Draft Product", slug: "draft-product", status: ProductStatus.Draft);
        var archivedProduct = CreateTestFilterIndex(
            name: "Archived Product", slug: "archived-product", status: ProductStatus.Archived);

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>
        {
            activeProduct, draftProduct, archivedProduct
        });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(1);
        result.Value.Products[0].Name.Should().Be("Active Product");
        result.Value.Total.Should().Be(1);
    }

    #endregion

    #region Category Filter

    [Fact]
    public async Task Handle_WithCategorySlug_ShouldLookupCategoryFromDbContext()
    {
        // Arrange
        var category = ProductCategory.Create("Electronics", "electronics", tenantId: TestTenantId);

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>());
        SetupProductCategories(new List<ProductCategory> { category });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(CategorySlug: "electronics");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Category lookup was performed via ProductCategories DbSet
        _dbContextMock.Verify(x => x.ProductCategories, Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentCategorySlug_ShouldReturnAllActiveProducts()
    {
        // Arrange
        var product = CreateTestFilterIndex(name: "Some Product", slug: "some-product");

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { product });
        SetupProductCategories(new List<ProductCategory>());
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(CategorySlug: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // When category not found, category filter is not applied, so all active products returned
        result.Value.Products.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithNullCategorySlug_ShouldNotQueryCategories()
    {
        // Arrange
        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>());
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(CategorySlug: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // ProductCategories should not be accessed when CategorySlug is null
        _dbContextMock.Verify(x => x.ProductCategories, Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyCategorySlug_ShouldNotQueryCategories()
    {
        // Arrange
        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>());
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(CategorySlug: "");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(x => x.ProductCategories, Times.Never);
    }

    #endregion

    #region Price Range Filter

    [Fact]
    public async Task Handle_WithPriceMinFilter_ShouldFilterProducts()
    {
        // Arrange
        var cheapProduct = CreateTestFilterIndex(
            name: "Cheap Product", slug: "cheap", minPrice: 50m, maxPrice: 80m);
        var expensiveProduct = CreateTestFilterIndex(
            name: "Expensive Product", slug: "expensive", minPrice: 500m, maxPrice: 1000m);

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { cheapProduct, expensiveProduct });
        SetupFacetCalculatorDependencies();

        // PriceMin filter: MaxPrice >= PriceMin
        var query = new FilterProductsQuery(PriceMin: 100m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Only the expensive product has MaxPrice >= 100m
        result.Value.Products.Should().HaveCount(1);
        result.Value.Products[0].Name.Should().Be("Expensive Product");
    }

    [Fact]
    public async Task Handle_WithPriceMaxFilter_ShouldFilterProducts()
    {
        // Arrange
        var cheapProduct = CreateTestFilterIndex(
            name: "Cheap Product", slug: "cheap", minPrice: 50m, maxPrice: 80m);
        var expensiveProduct = CreateTestFilterIndex(
            name: "Expensive Product", slug: "expensive", minPrice: 500m, maxPrice: 1000m);

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { cheapProduct, expensiveProduct });
        SetupFacetCalculatorDependencies();

        // PriceMax filter: MinPrice <= PriceMax
        var query = new FilterProductsQuery(PriceMax: 100m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(1);
        result.Value.Products[0].Name.Should().Be("Cheap Product");
    }

    [Fact]
    public async Task Handle_WithPriceRange_ShouldFilterProductsWithinRange()
    {
        // Arrange
        var cheapProduct = CreateTestFilterIndex(
            name: "Cheap", slug: "cheap", minPrice: 10m, maxPrice: 50m);
        var midProduct = CreateTestFilterIndex(
            name: "Mid", slug: "mid", minPrice: 100m, maxPrice: 300m);
        var expensiveProduct = CreateTestFilterIndex(
            name: "Expensive", slug: "expensive", minPrice: 500m, maxPrice: 1000m);

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>
        {
            cheapProduct, midProduct, expensiveProduct
        });
        SetupFacetCalculatorDependencies();

        // Price range [80, 400]: MaxPrice >= 80 AND MinPrice <= 400
        var query = new FilterProductsQuery(PriceMin: 80m, PriceMax: 400m);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(1);
        result.Value.Products[0].Name.Should().Be("Mid");
    }

    #endregion

    #region In-Stock Filter

    [Fact]
    public async Task Handle_WithInStockOnly_ShouldExcludeOutOfStockProducts()
    {
        // Arrange
        var inStockProduct = CreateTestFilterIndex(
            name: "In Stock", slug: "in-stock", inStock: true, totalStock: 10);
        var outOfStockProduct = CreateTestFilterIndex(
            name: "Out of Stock", slug: "out-of-stock", inStock: false, totalStock: 0);

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>
        {
            inStockProduct, outOfStockProduct
        });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(InStockOnly: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(1);
        result.Value.Products[0].Name.Should().Be("In Stock");
    }

    [Fact]
    public async Task Handle_WithInStockOnlyFalse_ShouldReturnAllProducts()
    {
        // Arrange
        var inStockProduct = CreateTestFilterIndex(
            name: "In Stock", slug: "in-stock", inStock: true);
        var outOfStockProduct = CreateTestFilterIndex(
            name: "Out of Stock", slug: "out-of-stock", inStock: false);

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>
        {
            inStockProduct, outOfStockProduct
        });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(InStockOnly: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(2);
    }

    #endregion

    #region Search Filter

    [Fact]
    public async Task Handle_WithSearchQuery_ShouldFilterBySearchText()
    {
        // Arrange
        var laptop = CreateTestFilterIndex(name: "Gaming Laptop", slug: "gaming-laptop");
        laptop.SetSearchText("Gaming Laptop high performance");
        var phone = CreateTestFilterIndex(name: "Smart Phone", slug: "smart-phone");
        phone.SetSearchText("Smart Phone mobile device");

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { laptop, phone });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(SearchQuery: "laptop");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(1);
        result.Value.Products[0].Name.Should().Be("Gaming Laptop");
    }

    [Fact]
    public async Task Handle_WithSearchQuery_ShouldBeCaseInsensitive()
    {
        // Arrange
        var laptop = CreateTestFilterIndex(name: "Gaming Laptop", slug: "gaming-laptop");
        laptop.SetSearchText("Gaming Laptop");

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { laptop });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(SearchQuery: "GAMING");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithNullSearchQuery_ShouldNotFilterBySearch()
    {
        // Arrange
        var product1 = CreateTestFilterIndex(name: "Product A", slug: "product-a");
        var product2 = CreateTestFilterIndex(name: "Product B", slug: "product-b");

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { product1, product2 });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(SearchQuery: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithEmptySearchQuery_ShouldNotFilterBySearch()
    {
        // Arrange
        var product1 = CreateTestFilterIndex(name: "Product A", slug: "product-a");
        var product2 = CreateTestFilterIndex(name: "Product B", slug: "product-b");

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { product1, product2 });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(SearchQuery: "");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithSearchQueryMatchingProductName_ShouldReturnMatch()
    {
        // Arrange
        var product = CreateTestFilterIndex(name: "MacBook Pro 16", slug: "macbook-pro-16");
        product.SetSearchText("MacBook Pro 16 Apple laptop");

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { product });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(SearchQuery: "macbook");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(1);
        result.Value.Products[0].Name.Should().Be("MacBook Pro 16");
    }

    #endregion

    #region Pagination

    [Fact]
    public async Task Handle_WithDefaultPagination_ShouldReturnFirstPageOf24()
    {
        // Arrange
        var products = Enumerable.Range(1, 30)
            .Select(i => CreateTestFilterIndex(
                name: $"Product {i:D2}",
                slug: $"product-{i}"))
            .ToList();

        SetupProductFilterIndexes(products);
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(24);
        result.Value.Products.Should().HaveCount(24);
        result.Value.Total.Should().Be(30);
    }

    [Fact]
    public async Task Handle_WithSecondPage_ShouldReturnRemainingProducts()
    {
        // Arrange
        var products = Enumerable.Range(1, 30)
            .Select(i => CreateTestFilterIndex(
                name: $"Product {i:D2}",
                slug: $"product-{i}"))
            .ToList();

        SetupProductFilterIndexes(products);
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(Page: 2, PageSize: 24);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.Products.Should().HaveCount(6); // 30 - 24 = 6 remaining
        result.Value.Total.Should().Be(30);
    }

    [Fact]
    public async Task Handle_WithCustomPageSize_ShouldRespectPageSize()
    {
        // Arrange
        var products = Enumerable.Range(1, 15)
            .Select(i => CreateTestFilterIndex(
                name: $"Product {i:D2}",
                slug: $"product-{i}"))
            .ToList();

        SetupProductFilterIndexes(products);
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(5);
        result.Value.PageSize.Should().Be(5);
        result.Value.Total.Should().Be(15);
    }

    [Fact]
    public async Task Handle_PageBeyondData_ShouldReturnEmptyProducts()
    {
        // Arrange
        var products = Enumerable.Range(1, 5)
            .Select(i => CreateTestFilterIndex(
                name: $"Product {i}",
                slug: $"product-{i}"))
            .ToList();

        SetupProductFilterIndexes(products);
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(Page: 10, PageSize: 24);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().BeEmpty();
        result.Value.Total.Should().Be(5);
    }

    #endregion

    #region Facet Calculator Integration

    [Fact]
    public async Task Handle_ShouldReturnFacetsInResult()
    {
        // Arrange - FacetCalculator is a real instance backed by mock DbContext
        var product = CreateTestFilterIndex(
            name: "Product With Price", slug: "product-price",
            minPrice: 100m, maxPrice: 500m);

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { product });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Facets.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyProducts_ShouldReturnEmptyFacets()
    {
        // Arrange
        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>());
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Facets.Should().NotBeNull();
        result.Value.Facets.Brands.Should().BeEmpty();
        result.Value.Facets.Attributes.Should().BeEmpty();
    }

    #endregion

    #region Applied Filters in Result

    [Fact]
    public async Task Handle_WithNoAttributeFilters_ShouldReturnEmptyAppliedFilters()
    {
        // Arrange
        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>());
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AppliedFilters.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithAttributeFilters_ShouldReturnAppliedFilters()
    {
        // Arrange
        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>());
        SetupFacetCalculatorDependencies();

        var attributeFilters = new Dictionary<string, List<string>>
        {
            ["color"] = new List<string> { "red", "blue" },
            ["size"] = new List<string> { "m" }
        };

        var query = new FilterProductsQuery(AttributeFilters: attributeFilters);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AppliedFilters.Should().ContainKey("color");
        result.Value.AppliedFilters["color"].Should().Contain("red");
        result.Value.AppliedFilters["color"].Should().Contain("blue");
        result.Value.AppliedFilters.Should().ContainKey("size");
    }

    #endregion

    #region Combined Filters

    [Fact]
    public async Task Handle_WithMultipleFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var matchingProduct = CreateTestFilterIndex(
            name: "Matching Product",
            slug: "matching",
            minPrice: 100m,
            maxPrice: 200m,
            inStock: true);
        matchingProduct.SetSearchText("Matching Product laptop gaming");

        var nonMatchingPrice = CreateTestFilterIndex(
            name: "Too Expensive",
            slug: "expensive",
            minPrice: 5000m,
            maxPrice: 10000m,
            inStock: true);
        nonMatchingPrice.SetSearchText("Too Expensive laptop");

        var nonMatchingStock = CreateTestFilterIndex(
            name: "Out of Stock Laptop",
            slug: "oos-laptop",
            minPrice: 100m,
            maxPrice: 200m,
            inStock: false);
        nonMatchingStock.SetSearchText("Out of Stock Laptop");

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>
        {
            matchingProduct, nonMatchingPrice, nonMatchingStock
        });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery(
            SearchQuery: "laptop",
            PriceMax: 500m,
            InStockOnly: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(1);
        result.Value.Products[0].Name.Should().Be("Matching Product");
    }

    #endregion

    #region Product DTO Mapping

    [Fact]
    public async Task Handle_ShouldMapProductFieldsCorrectly()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = CreateTestFilterIndex(
            productId: productId,
            name: "Test Laptop",
            slug: "test-laptop",
            minPrice: 999m,
            maxPrice: 1499m,
            currency: "VND",
            inStock: true,
            totalStock: 25);

        SetupProductFilterIndexes(new List<ProductFilterIndexEntity> { product });
        SetupFacetCalculatorDependencies();

        var query = new FilterProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Products.Should().HaveCount(1);

        var dto = result.Value.Products[0];
        dto.Id.Should().Be(productId);
        dto.Name.Should().Be("Test Laptop");
        dto.Slug.Should().Be("test-laptop");
        dto.Status.Should().Be(ProductStatus.Active);
        dto.MinPrice.Should().Be(999m);
        dto.MaxPrice.Should().Be(1499m);
        dto.Currency.Should().Be("VND");
        dto.InStock.Should().BeTrue();
        dto.TotalStock.Should().Be(25);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItThrough()
    {
        // Arrange
        SetupProductFilterIndexes(new List<ProductFilterIndexEntity>());
        SetupFacetCalculatorDependencies();

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var query = new FilterProductsQuery();

        // Act
        var result = await _handler.Handle(query, token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldHaveCorrectDefaultQueryValues()
    {
        // Arrange & Assert - verify default values on the query record
        var query = new FilterProductsQuery();

        query.Sort.Should().Be("newest");
        query.Order.Should().Be(SortOrder.Descending);
        query.Page.Should().Be(1);
        query.PageSize.Should().Be(24);
        query.InStockOnly.Should().BeFalse();
        query.CategorySlug.Should().BeNull();
        query.Brands.Should().BeNull();
        query.SearchQuery.Should().BeNull();
        query.PriceMin.Should().BeNull();
        query.PriceMax.Should().BeNull();
        query.AttributeFilters.Should().BeNull();
    }

    #endregion
}
