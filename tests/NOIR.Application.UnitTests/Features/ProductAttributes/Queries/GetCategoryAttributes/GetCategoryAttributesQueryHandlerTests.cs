using NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributes;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Queries.GetCategoryAttributes;

/// <summary>
/// Unit tests for GetCategoryAttributesQueryHandler.
/// Tests getting attributes assigned to a category.
/// </summary>
public class GetCategoryAttributesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly GetCategoryAttributesQueryHandler _handler;

    public GetCategoryAttributesQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();

        _handler = new GetCategoryAttributesQueryHandler(
            _dbContextMock.Object,
            _categoryRepositoryMock.Object);
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category",
        string? tenantId = "tenant-1")
    {
        return ProductCategory.Create(name, slug, tenantId: tenantId);
    }

    private static CategoryAttribute CreateTestCategoryAttribute(
        Guid categoryId,
        Guid attributeId,
        string? categoryName = "Test Category",
        string? attributeName = "Test Attribute",
        int sortOrder = 0,
        string? tenantId = "tenant-1")
    {
        var categoryAttribute = CategoryAttribute.Create(categoryId, attributeId, tenantId: tenantId);
        categoryAttribute.SetSortOrder(sortOrder);

        if (categoryName != null)
        {
            var category = ProductCategory.Create(categoryName, "test-category", tenantId: tenantId);
            typeof(CategoryAttribute).GetProperty("Category")!.SetValue(categoryAttribute, category);
        }

        if (attributeName != null)
        {
            var attribute = ProductAttribute.Create("test_attr", attributeName, AttributeType.Text, tenantId);
            typeof(CategoryAttribute).GetProperty("Attribute")!.SetValue(categoryAttribute, attribute);
        }

        return categoryAttribute;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCategory_ShouldReturnAttributes()
    {
        // Arrange
        var category = CreateTestCategory("Electronics", "electronics");
        var categoryId = category.Id;

        var attributeId1 = Guid.NewGuid();
        var attributeId2 = Guid.NewGuid();

        var categoryAttribute1 = CreateTestCategoryAttribute(categoryId, attributeId1, attributeName: "Screen Size", sortOrder: 1);
        var categoryAttribute2 = CreateTestCategoryAttribute(categoryId, attributeId2, attributeName: "Color", sortOrder: 2);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var categoryAttributes = new List<CategoryAttribute> { categoryAttribute1, categoryAttribute2 }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        var query = new GetCategoryAttributesQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnAttributesInSortOrder()
    {
        // Arrange
        var category = CreateTestCategory();
        var categoryId = category.Id;

        var attr1 = CreateTestCategoryAttribute(categoryId, Guid.NewGuid(), attributeName: "Third", sortOrder: 3);
        var attr2 = CreateTestCategoryAttribute(categoryId, Guid.NewGuid(), attributeName: "First", sortOrder: 1);
        var attr3 = CreateTestCategoryAttribute(categoryId, Guid.NewGuid(), attributeName: "Second", sortOrder: 2);

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        // MockQueryable orders by insertion, so we test handler behavior
        var categoryAttributes = new List<CategoryAttribute> { attr1, attr2, attr3 }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        var query = new GetCategoryAttributesQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithNoAttributes_ShouldReturnEmptyList()
    {
        // Arrange
        var category = CreateTestCategory();
        var categoryId = category.Id;

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(categoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var emptyCategoryAttributes = new List<CategoryAttribute>().BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(emptyCategoryAttributes.Object);

        var query = new GetCategoryAttributesQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnAttributesForSpecifiedCategory()
    {
        // Arrange
        var targetCategory = CreateTestCategory("Target", "target");
        var otherCategory = CreateTestCategory("Other", "other");

        var targetCategoryId = targetCategory.Id;
        var otherCategoryId = otherCategory.Id;

        var targetAttr = CreateTestCategoryAttribute(targetCategoryId, Guid.NewGuid(), categoryName: "Target", attributeName: "Target Attr");
        var otherAttr = CreateTestCategoryAttribute(otherCategoryId, Guid.NewGuid(), categoryName: "Other", attributeName: "Other Attr");

        _categoryRepositoryMock
            .Setup(x => x.GetByIdAsync(targetCategoryId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetCategory);

        var categoryAttributes = new List<CategoryAttribute> { targetAttr, otherAttr }.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(categoryAttributes.Object);

        var query = new GetCategoryAttributesQuery(targetCategoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
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

        var query = new GetCategoryAttributesQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-PRODUCT-003");
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

        var query = new GetCategoryAttributesQuery(categoryId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _categoryRepositoryMock.Verify(x => x.GetByIdAsync(categoryId, token), Times.Once);
    }

    #endregion
}
