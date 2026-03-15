using NOIR.Application.Features.ProductFilter.DTOs;
using NOIR.Application.Features.ProductFilter.Queries.GetCategoryFilters;
using ProductFilterIndexEntity = NOIR.Domain.Entities.Product.ProductFilterIndex;

namespace NOIR.Application.UnitTests.Features.ProductFilter;

/// <summary>
/// Unit tests for GetCategoryFiltersQueryHandler.
/// Tests category filter retrieval with brand, price, and attribute filter definitions.
/// Due to complex IQueryable chains with GroupBy/Include/ThenInclude, tests focus on key paths.
/// </summary>
public class GetCategoryFiltersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ILogger<GetCategoryFiltersQueryHandler>> _loggerMock;
    private readonly GetCategoryFiltersQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetCategoryFiltersQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _loggerMock = new Mock<ILogger<GetCategoryFiltersQueryHandler>>();

        _handler = new GetCategoryFiltersQueryHandler(
            _dbContextMock.Object,
            _loggerMock.Object);
    }

    private void SetupProductCategoriesDbSet(List<ProductCategory> categories)
    {
        var mockDbSet = categories.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductCategories).Returns(mockDbSet.Object);
    }

    private void SetupProductFilterIndexesDbSet(List<ProductFilterIndexEntity> indexes)
    {
        var mockDbSet = indexes.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductFilterIndexes).Returns(mockDbSet.Object);
    }

    private void SetupCategoryAttributesDbSet(List<CategoryAttribute> categoryAttributes)
    {
        var mockDbSet = categoryAttributes.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(mockDbSet.Object);
    }

    private void SetupProductAttributesDbSet(List<ProductAttribute> attributes)
    {
        var mockDbSet = attributes.BuildMockDbSet();
        _dbContextMock.Setup(x => x.ProductAttributes).Returns(mockDbSet.Object);
    }

    private void SetupEmptyDependencies()
    {
        SetupProductFilterIndexesDbSet(new List<ProductFilterIndexEntity>());
        SetupCategoryAttributesDbSet(new List<CategoryAttribute>());
        SetupProductAttributesDbSet(new List<ProductAttribute>());
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldCreateInstance()
    {
        // Assert
        _handler.ShouldNotBeNull();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        SetupProductCategoriesDbSet(new List<ProductCategory>());

        var query = new GetCategoryFiltersQuery("non-existent-category");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe(ErrorCodes.Product.CategoryNotFound);
        result.Error.Message.ShouldContain("non-existent-category");
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldIncludeSlugInErrorMessage()
    {
        // Arrange
        SetupProductCategoriesDbSet(new List<ProductCategory>());

        var query = new GetCategoryFiltersQuery("missing-slug-xyz");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Message.ShouldContain("missing-slug-xyz");
    }

    [Fact]
    public async Task Handle_WithEmptyDbSet_ShouldReturnNotFound()
    {
        // Arrange
        SetupProductCategoriesDbSet(new List<ProductCategory>());

        var query = new GetCategoryFiltersQuery("anything");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenCategoryFound_ShouldReturnSuccessWithCategoryDetails()
    {
        // Arrange
        var category = ProductCategory.Create("Electronics", "electronics", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupEmptyDependencies();

        var query = new GetCategoryFiltersQuery("electronics");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.CategoryId.ShouldBe(category.Id);
        result.Value.CategoryName.ShouldBe("Electronics");
        result.Value.CategorySlug.ShouldBe("electronics");
        result.Value.Filters.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_WhenCategoryFound_ShouldReturnNonNullFilters()
    {
        // Arrange
        var category = ProductCategory.Create("Clothing", "clothing", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupEmptyDependencies();

        var query = new GetCategoryFiltersQuery("clothing");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Filters.ShouldNotBeNull();
    }

    #endregion

    #region In-Stock Filter (Always Included)

    [Fact]
    public async Task Handle_WhenCategoryFound_ShouldAlwaysIncludeInStockFilter()
    {
        // Arrange
        var category = ProductCategory.Create("Books", "books", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupEmptyDependencies();

        var query = new GetCategoryFiltersQuery("books");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var inStockFilter = result.Value.Filters.FirstOrDefault(f => f.Code == "in_stock");
        inStockFilter.ShouldNotBeNull();
        inStockFilter!.Name.ShouldBe("Availability");
        inStockFilter.Type.ShouldBe("boolean");
        inStockFilter.DisplayType.ShouldBe(FacetDisplayType.Boolean);
        inStockFilter.Values.Count().ShouldBe(2);
        inStockFilter.Values.ShouldContain(v => v.Value == "true" && v.Label == "In Stock");
        inStockFilter.Values.ShouldContain(v => v.Value == "false" && v.Label == "Out of Stock");
    }

    [Fact]
    public async Task Handle_InStockFilter_ShouldHaveCorrectDisplayType()
    {
        // Arrange
        var category = ProductCategory.Create("Toys", "toys", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupEmptyDependencies();

        var query = new GetCategoryFiltersQuery("toys");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var inStockFilter = result.Value.Filters.First(f => f.Code == "in_stock");
        inStockFilter.DisplayType.ShouldBe(FacetDisplayType.Boolean);
    }

    [Fact]
    public async Task Handle_InStockFilter_ShouldHaveTwoValues()
    {
        // Arrange
        var category = ProductCategory.Create("Food", "food", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupEmptyDependencies();

        var query = new GetCategoryFiltersQuery("food");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var inStockFilter = result.Value.Filters.First(f => f.Code == "in_stock");
        inStockFilter.Values.Count().ShouldBe(2);

        var trueValue = inStockFilter.Values.First(v => v.Value == "true");
        trueValue.Label.ShouldBe("In Stock");

        var falseValue = inStockFilter.Values.First(v => v.Value == "false");
        falseValue.Label.ShouldBe("Out of Stock");
    }

    #endregion

    #region Filter Type Mappings (CreateFilterDefinition)

    [Fact]
    public async Task Handle_ColorAttribute_ShouldMapToColorDisplayType()
    {
        // Arrange
        var category = ProductCategory.Create("Shoes", "shoes", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupProductFilterIndexesDbSet(new List<ProductFilterIndexEntity>());
        SetupCategoryAttributesDbSet(new List<CategoryAttribute>());

        // Global filterable color attribute
        var colorAttr = ProductAttribute.Create("color", "Color", AttributeType.Color, TestTenantId);
        colorAttr.SetBehaviorFlags(isFilterable: true, isSearchable: true, isRequired: false, isVariantAttribute: false);
        SetupProductAttributesDbSet(new List<ProductAttribute> { colorAttr });

        var query = new GetCategoryFiltersQuery("shoes");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Filters.ShouldContain(f => f.Code == "color",
            "a Color-type attribute should produce a filter with code 'color'");
        var colorFilter = result.Value.Filters.First(f => f.Code == "color");
        colorFilter.DisplayType.ShouldBe(FacetDisplayType.Color);
        colorFilter.Type.ShouldBe("color");
    }

    [Fact]
    public async Task Handle_BooleanAttribute_ShouldMapToBooleanDisplayType()
    {
        // Arrange
        var category = ProductCategory.Create("Tech", "tech", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupProductFilterIndexesDbSet(new List<ProductFilterIndexEntity>());
        SetupCategoryAttributesDbSet(new List<CategoryAttribute>());

        // Global filterable boolean attribute
        var boolAttr = ProductAttribute.Create("wireless", "Wireless", AttributeType.Boolean, TestTenantId);
        boolAttr.SetBehaviorFlags(isFilterable: true, isSearchable: false, isRequired: false, isVariantAttribute: false);
        SetupProductAttributesDbSet(new List<ProductAttribute> { boolAttr });

        var query = new GetCategoryFiltersQuery("tech");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Filters.ShouldContain(f => f.Code == "wireless",
            "a Boolean-type attribute should produce a filter with code 'wireless'");
        var wirelessFilter = result.Value.Filters.First(f => f.Code == "wireless");
        wirelessFilter.DisplayType.ShouldBe(FacetDisplayType.Boolean);
        wirelessFilter.Type.ShouldBe("boolean");
    }

    [Fact]
    public async Task Handle_NumberAttribute_ShouldMapToRangeDisplayType()
    {
        // Arrange
        var category = ProductCategory.Create("Laptops", "laptops", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupProductFilterIndexesDbSet(new List<ProductFilterIndexEntity>());
        SetupCategoryAttributesDbSet(new List<CategoryAttribute>());

        // Global filterable number attribute
        var numberAttr = ProductAttribute.Create("screen_size", "Screen Size", AttributeType.Number, TestTenantId);
        numberAttr.SetBehaviorFlags(isFilterable: true, isSearchable: false, isRequired: false, isVariantAttribute: false);
        numberAttr.SetTypeConfiguration(unit: "inch", validationRegex: null, minValue: 10m, maxValue: 100m, maxLength: null);
        SetupProductAttributesDbSet(new List<ProductAttribute> { numberAttr });

        var query = new GetCategoryFiltersQuery("laptops");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Filters.ShouldContain(f => f.Code == "screen_size",
            "a Number-type attribute should produce a filter with code 'screen_size'");
        var screenFilter = result.Value.Filters.First(f => f.Code == "screen_size");
        screenFilter.DisplayType.ShouldBe(FacetDisplayType.Range);
        screenFilter.Unit.ShouldBe("inch");
        screenFilter.Min.ShouldBe(10m);
        screenFilter.Max.ShouldBe(100m);
    }

    [Fact]
    public async Task Handle_SelectAttribute_ShouldMapToCheckboxDisplayType()
    {
        // Arrange
        var category = ProductCategory.Create("Monitors", "monitors", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupProductFilterIndexesDbSet(new List<ProductFilterIndexEntity>());
        SetupCategoryAttributesDbSet(new List<CategoryAttribute>());

        // Global filterable select attribute
        var selectAttr = ProductAttribute.Create("resolution", "Resolution", AttributeType.Select, TestTenantId);
        selectAttr.SetBehaviorFlags(isFilterable: true, isSearchable: false, isRequired: false, isVariantAttribute: false);
        SetupProductAttributesDbSet(new List<ProductAttribute> { selectAttr });

        var query = new GetCategoryFiltersQuery("monitors");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Filters.ShouldContain(f => f.Code == "resolution",
            "a Select-type attribute should produce a filter with code 'resolution'");
        var resolutionFilter = result.Value.Filters.First(f => f.Code == "resolution");
        resolutionFilter.DisplayType.ShouldBe(FacetDisplayType.Checkbox);
        resolutionFilter.Type.ShouldBe("select");
    }

    #endregion

    #region Global Attributes Fallback

    [Fact]
    public async Task Handle_WithNoCategoryAttributes_ShouldFallbackToGlobalAttributes()
    {
        // Arrange
        var category = ProductCategory.Create("New Category", "new-category", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupProductFilterIndexesDbSet(new List<ProductFilterIndexEntity>());

        // No category-specific attributes
        SetupCategoryAttributesDbSet(new List<CategoryAttribute>());

        // Global filterable attribute exists
        var globalAttr = ProductAttribute.Create("material", "Material", AttributeType.Select, TestTenantId);
        globalAttr.SetBehaviorFlags(isFilterable: true, isSearchable: false, isRequired: false, isVariantAttribute: false);
        SetupProductAttributesDbSet(new List<ProductAttribute> { globalAttr });

        var query = new GetCategoryFiltersQuery("new-category");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // Should include global attributes when no category-specific ones exist
        result.Value.Filters.ShouldContain(f => f.Code == "material",
            "global filterable attributes should be included when no category-specific ones exist");
        var materialFilter = result.Value.Filters.First(f => f.Code == "material");
        materialFilter.Name.ShouldBe("Material");
    }

    [Fact]
    public async Task Handle_OnlyActiveFilterableAttributes_ShouldBeIncluded()
    {
        // Arrange
        var category = ProductCategory.Create("Test Category", "test-cat", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupProductFilterIndexesDbSet(new List<ProductFilterIndexEntity>());
        SetupCategoryAttributesDbSet(new List<CategoryAttribute>());

        // One active+filterable, one inactive, one non-filterable
        var activeFilterable = ProductAttribute.Create("active_filter", "Active Filter", AttributeType.Select, TestTenantId);
        activeFilterable.SetBehaviorFlags(isFilterable: true, isSearchable: false, isRequired: false, isVariantAttribute: false);

        var inactiveAttr = ProductAttribute.Create("inactive", "Inactive", AttributeType.Select, TestTenantId);
        inactiveAttr.SetBehaviorFlags(isFilterable: true, isSearchable: false, isRequired: false, isVariantAttribute: false);
        inactiveAttr.SetActive(false);

        var nonFilterable = ProductAttribute.Create("non_filter", "Non Filterable", AttributeType.Text, TestTenantId);
        nonFilterable.SetBehaviorFlags(isFilterable: false, isSearchable: true, isRequired: false, isVariantAttribute: false);

        SetupProductAttributesDbSet(new List<ProductAttribute>
        {
            activeFilterable, inactiveAttr, nonFilterable
        });

        var query = new GetCategoryFiltersQuery("test-cat");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // Should not include inactive or non-filterable attributes
        result.Value.Filters.ShouldNotContain(f => f.Code == "inactive");
        result.Value.Filters.ShouldNotContain(f => f.Code == "non_filter");
    }

    #endregion

    #region Multiple Categories

    [Fact]
    public async Task Handle_ShouldReturnCorrectCategoryBySlug()
    {
        // Arrange
        var electronics = ProductCategory.Create("Electronics", "electronics", tenantId: TestTenantId);
        var fashion = ProductCategory.Create("Fashion", "fashion", tenantId: TestTenantId);

        SetupProductCategoriesDbSet(new List<ProductCategory> { electronics, fashion });
        SetupEmptyDependencies();

        var query = new GetCategoryFiltersQuery("fashion");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.CategoryName.ShouldBe("Fashion");
        result.Value.CategorySlug.ShouldBe("fashion");
        result.Value.CategoryId.ShouldBe(fashion.Id);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldNotThrow()
    {
        // Arrange
        var category = ProductCategory.Create("Test", "test", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupEmptyDependencies();

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var query = new GetCategoryFiltersQuery("test");

        // Act
        var result = await _handler.Handle(query, token);

        // Assert
        result.IsSuccess.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_FiltersShouldAlwaysContainInStock()
    {
        // Arrange - even with empty data, in_stock should be present
        var category = ProductCategory.Create("Empty", "empty", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });
        SetupEmptyDependencies();

        var query = new GetCategoryFiltersQuery("empty");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Filters.ShouldContain(f => f.Code == "in_stock");
    }

    [Fact]
    public async Task Handle_CategorySlugLookup_ShouldBeCaseSensitive()
    {
        // Arrange - slugs are lowercase per ProductCategory.Create
        var category = ProductCategory.Create("Electronics", "electronics", tenantId: TestTenantId);
        SetupProductCategoriesDbSet(new List<ProductCategory> { category });

        // NOTE: This test verifies LINQ-to-Objects behavior (case-sensitive string ==).
        // In production with EF Core, case-sensitivity depends on database collation.
        // Validators should enforce lowercase slugs to avoid ambiguity.
        var query = new GetCategoryFiltersQuery("ELECTRONICS");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    #endregion
}
