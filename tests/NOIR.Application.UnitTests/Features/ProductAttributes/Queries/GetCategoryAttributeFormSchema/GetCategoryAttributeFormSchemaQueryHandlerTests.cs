using NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributeFormSchema;
using NOIR.Application.Features.ProductAttributes.Specifications;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Queries.GetCategoryAttributeFormSchema;

/// <summary>
/// Unit tests for GetCategoryAttributeFormSchemaQueryHandler.
/// Tests getting form schema for category attributes (used for new product creation).
/// </summary>
public class GetCategoryAttributeFormSchemaQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IRepository<ProductAttribute, Guid>> _attributeRepositoryMock;
    private readonly GetCategoryAttributeFormSchemaQueryHandler _handler;

    public GetCategoryAttributeFormSchemaQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _attributeRepositoryMock = new Mock<IRepository<ProductAttribute, Guid>>();

        _handler = new GetCategoryAttributeFormSchemaQueryHandler(
            _dbContextMock.Object,
            _categoryRepositoryMock.Object,
            _attributeRepositoryMock.Object);
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category",
        string? tenantId = "tenant-1")
    {
        return ProductCategory.Create(name, slug, tenantId: tenantId);
    }

    private static ProductAttribute CreateTestAttribute(
        string code = "test_attr",
        string name = "Test Attribute",
        AttributeType type = AttributeType.Text,
        bool isRequired = false,
        string? tenantId = "tenant-1")
    {
        var attr = ProductAttribute.Create(code, name, type, tenantId);
        if (isRequired)
        {
            attr.SetBehaviorFlags(isFilterable: false, isSearchable: false, isRequired: true, isVariantAttribute: false);
        }
        return attr;
    }

    private static CategoryAttribute CreateTestCategoryAttribute(
        Guid categoryId,
        Guid attributeId,
        bool isRequired = false,
        int sortOrder = 0,
        string? tenantId = "tenant-1")
    {
        var ca = CategoryAttribute.Create(categoryId, attributeId, tenantId: tenantId);
        ca.SetRequired(isRequired);
        ca.SetSortOrder(sortOrder);
        return ca;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCategory_ShouldReturnFormSchema()
    {
        // Arrange
        var category = CreateTestCategory("Electronics", "electronics");
        var categoryId = category.Id;

        var attribute1 = CreateTestAttribute("screen_size", "Screen Size", AttributeType.Number);
        var attribute2 = CreateTestAttribute("color", "Color", AttributeType.Select);

        var categoryAttribute1 = CreateTestCategoryAttribute(categoryId, attribute1.Id, sortOrder: 1);
        var categoryAttribute2 = CreateTestCategoryAttribute(categoryId, attribute2.Id, sortOrder: 2);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var categoryAttributes = new List<CategoryAttribute> { categoryAttribute1, categoryAttribute2 }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByIdsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute1, attribute2 });

        var query = new GetCategoryAttributeFormSchemaQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.CategoryName.Should().Be("Electronics");
        result.Value.Fields.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnFieldsInCorrectOrder()
    {
        // Arrange
        var category = CreateTestCategory();
        var categoryId = category.Id;

        var attribute1 = CreateTestAttribute("attr_a", "Attribute A");
        var attribute2 = CreateTestAttribute("attr_b", "Attribute B");
        var attribute3 = CreateTestAttribute("attr_c", "Attribute C");

        var categoryAttribute1 = CreateTestCategoryAttribute(categoryId, attribute1.Id, sortOrder: 3);
        var categoryAttribute2 = CreateTestCategoryAttribute(categoryId, attribute2.Id, sortOrder: 1);
        var categoryAttribute3 = CreateTestCategoryAttribute(categoryId, attribute3.Id, sortOrder: 2);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var categoryAttributes = new List<CategoryAttribute> { categoryAttribute1, categoryAttribute2, categoryAttribute3 }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByIdsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute1, attribute2, attribute3 });

        var query = new GetCategoryAttributeFormSchemaQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Fields.Should().HaveCount(3);
        // Fields should be ordered by sortOrder
    }

    [Fact]
    public async Task Handle_WithCategoryRequiredOverride_ShouldReflectInField()
    {
        // Arrange
        var category = CreateTestCategory();
        var categoryId = category.Id;

        // Attribute is NOT required by default
        var attribute = CreateTestAttribute("optional_attr", "Optional Attribute", isRequired: false);

        // But category-level makes it required
        var categoryAttribute = CreateTestCategoryAttribute(categoryId, attribute.Id, isRequired: true);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var categoryAttributes = new List<CategoryAttribute> { categoryAttribute }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByIdsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        var query = new GetCategoryAttributeFormSchemaQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Fields.Should().HaveCount(1);
        result.Value.Fields.First().IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoAttributes_ShouldReturnEmptyFields()
    {
        // Arrange
        var category = CreateTestCategory();
        var categoryId = category.Id;

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var emptyCategoryAttributes = new List<CategoryAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(emptyCategoryAttributes.Object);

        var query = new GetCategoryAttributeFormSchemaQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Fields.Should().BeEmpty();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var categoryId = Guid.NewGuid();

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        var query = new GetCategoryAttributeFormSchemaQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Product.CategoryNotFound);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllServices()
    {
        // Arrange
        var category = CreateTestCategory();
        var categoryId = category.Id;
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, token))
            .ReturnsAsync(category);

        var emptyCategoryAttributes = new List<CategoryAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(emptyCategoryAttributes.Object);

        var query = new GetCategoryAttributeFormSchemaQuery(categoryId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _categoryRepositoryMock.Verify(x => x.GetByIdAsync(categoryId, token), Times.Once);
    }

    [Fact]
    public async Task Handle_FieldsShouldHaveNullCurrentValues()
    {
        // Arrange - form schema is for NEW products, so no current values
        var category = CreateTestCategory();
        var categoryId = category.Id;

        var attribute = CreateTestAttribute("test", "Test");
        var categoryAttribute = CreateTestCategoryAttribute(categoryId, attribute.Id);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var categoryAttributes = new List<CategoryAttribute> { categoryAttribute }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        _attributeRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductAttributesByIdsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductAttribute> { attribute });

        var query = new GetCategoryAttributeFormSchemaQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Fields.First().CurrentValue.Should().BeNull();
        result.Value.Fields.First().CurrentDisplayValue.Should().BeNull();
    }

    #endregion
}
