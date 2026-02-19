using NOIR.Application.Features.ProductAttributes;
using NOIR.Application.Features.ProductAttributes.DTOs;
using NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributeById;

namespace NOIR.Application.UnitTests.Features.ProductAttributes.Queries.GetCategoryAttributeById;

/// <summary>
/// Unit tests for GetCategoryAttributeByIdQueryHandler.
/// Tests retrieving a category-attribute link by its ID.
/// NOTE: Navigation properties (Category, Attribute) are pre-populated via reflection
/// in test helpers because MockQueryable.Moq does not execute EF Include() chains.
/// The DTO mapping from those navigation properties is verified, but the Include()
/// call itself is not. Integration tests cover the full query path.
/// </summary>
public class GetCategoryAttributeByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly GetCategoryAttributeByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetCategoryAttributeByIdQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new GetCategoryAttributeByIdQueryHandler(_dbContextMock.Object);
    }

    private static CategoryAttribute CreateTestCategoryAttribute(
        Guid? categoryId = null,
        Guid? attributeId = null,
        string categoryName = "Electronics",
        string attributeName = "Screen Size",
        string attributeCode = "screen_size",
        bool isRequired = false,
        int sortOrder = 0)
    {
        var catId = categoryId ?? Guid.NewGuid();
        var attrId = attributeId ?? Guid.NewGuid();

        var categoryAttribute = CategoryAttribute.Create(catId, attrId, isRequired, sortOrder, TestTenantId);

        // Set navigation properties via reflection (private setters)
        var category = ProductCategory.Create(categoryName, categoryName.ToLowerInvariant().Replace(" ", "-"), tenantId: TestTenantId);
        typeof(ProductCategory).GetProperty("Id")!.SetValue(category, catId);
        typeof(CategoryAttribute).GetProperty("Category")!.SetValue(categoryAttribute, category);

        var attribute = ProductAttribute.Create(attributeCode, attributeName, AttributeType.Text, TestTenantId);
        typeof(ProductAttribute).GetProperty("Id")!.SetValue(attribute, attrId);
        typeof(CategoryAttribute).GetProperty("Attribute")!.SetValue(categoryAttribute, attribute);

        return categoryAttribute;
    }

    private void SetupDbSet(List<CategoryAttribute> items)
    {
        var mockDbSet = items.BuildMockDbSet();
        _dbContextMock.Setup(x => x.CategoryAttributes).Returns(mockDbSet.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_CategoryAttributeExists_ReturnsSuccess()
    {
        // Arrange
        var categoryAttribute = CreateTestCategoryAttribute();
        SetupDbSet(new List<CategoryAttribute> { categoryAttribute });

        var query = new GetCategoryAttributeByIdQuery(categoryAttribute.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_CategoryAttributeExists_ReturnsMappedDto()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();
        var categoryAttribute = CreateTestCategoryAttribute(
            categoryId: categoryId,
            attributeId: attributeId,
            categoryName: "Clothing",
            attributeName: "Size",
            attributeCode: "size",
            isRequired: true,
            sortOrder: 5);

        SetupDbSet(new List<CategoryAttribute> { categoryAttribute });

        var query = new GetCategoryAttributeByIdQuery(categoryAttribute.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Id.Should().Be(categoryAttribute.Id);
        dto.CategoryId.Should().Be(categoryId);
        dto.CategoryName.Should().Be("Clothing");
        dto.AttributeId.Should().Be(attributeId);
        dto.AttributeName.Should().Be("Size");
        dto.AttributeCode.Should().Be("size");
        dto.IsRequired.Should().BeTrue();
        dto.SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithMultipleRecords_ReturnsCorrectOne()
    {
        // Arrange
        var target = CreateTestCategoryAttribute(attributeName: "Color");
        var other = CreateTestCategoryAttribute(attributeName: "Weight");

        SetupDbSet(new List<CategoryAttribute> { target, other });

        var query = new GetCategoryAttributeByIdQuery(target.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(target.Id);
    }

    [Fact]
    public async Task Handle_NotRequired_ReturnsDtoWithIsRequiredFalse()
    {
        // Arrange
        var categoryAttribute = CreateTestCategoryAttribute(isRequired: false);
        SetupDbSet(new List<CategoryAttribute> { categoryAttribute });

        var query = new GetCategoryAttributeByIdQuery(categoryAttribute.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsRequired.Should().BeFalse();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_CategoryAttributeNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        SetupDbSet(new List<CategoryAttribute>());

        var query = new GetCategoryAttributeByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-CAT-ATTR-001");
        result.Error.Message.Should().Contain(nonExistentId.ToString());
    }

    [Fact]
    public async Task Handle_WithEmptyGuid_ReturnsNotFoundError()
    {
        // Arrange
        SetupDbSet(new List<CategoryAttribute>());

        var query = new GetCategoryAttributeByIdQuery(Guid.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CAT-ATTR-001");
    }

    [Fact]
    public async Task Handle_WhenIdDoesNotMatchAnyRecord_ReturnsNotFoundWithCorrectMessage()
    {
        // Arrange
        var existingAttribute = CreateTestCategoryAttribute();
        SetupDbSet(new List<CategoryAttribute> { existingAttribute });

        var differentId = Guid.NewGuid();
        var query = new GetCategoryAttributeByIdQuery(differentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain(differentId.ToString());
        result.Error.Message.Should().Contain("not found");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldNotThrowWhenNotCancelled()
    {
        // Arrange
        var categoryAttribute = CreateTestCategoryAttribute();
        SetupDbSet(new List<CategoryAttribute> { categoryAttribute });

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var query = new GetCategoryAttributeByIdQuery(categoryAttribute.Id);

        // Act
        var result = await _handler.Handle(query, token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
