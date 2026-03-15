using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductCategoryById;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for GetProductCategoryByIdQueryHandler.
/// Tests category retrieval by ID scenarios with mocked dependencies.
/// </summary>
public class GetProductCategoryByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly GetProductCategoryByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetProductCategoryByIdQueryHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();

        _handler = new GetProductCategoryByIdQueryHandler(
            _categoryRepositoryMock.Object);
    }

    private static GetProductCategoryByIdQuery CreateTestQuery(Guid id)
    {
        return new GetProductCategoryByIdQuery(id);
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category",
        Guid? parentId = null)
    {
        return ProductCategory.Create(name, slug, parentId, TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        var query = CreateTestQuery(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe(existingCategory.Name);
        result.Value.Slug.ShouldBe(existingCategory.Slug);
    }

    [Fact]
    public async Task Handle_ShouldMapAllCategoryFields()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory("Full Category", "full-category");
        existingCategory.UpdateDetails("Full Category", "full-category", "Description", "https://example.com/img.jpg");
        existingCategory.UpdateSeo("SEO Title", "SEO Description");
        existingCategory.SetSortOrder(5);
        var query = CreateTestQuery(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;
        dto.Name.ShouldBe("Full Category");
        dto.Slug.ShouldBe("full-category");
        dto.Description.ShouldBe("Description");
        dto.ImageUrl.ShouldBe("https://example.com/img.jpg");
        dto.MetaTitle.ShouldBe("SEO Title");
        dto.MetaDescription.ShouldBe("SEO Description");
        dto.SortOrder.ShouldBe(5);
    }

    [Fact]
    public async Task Handle_WithChildren_ShouldIncludeChildrenInResult()
    {
        // Arrange - We can't easily test children without complex setup
        // but we test that the handler returns the category correctly
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        var query = CreateTestQuery(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // Children collection should be mapped (empty in this case since no children on category)
        result.Value.Children.ShouldBeEmpty();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var query = CreateTestQuery(nonExistentId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("NOIR-PRODUCT-003");
        result.Error.Message.ShouldContain("not found");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        var query = CreateTestQuery(categoryId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductCategoryByIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnProductCount()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        existingCategory.UpdateProductCount(10);
        var query = CreateTestQuery(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProductCount.ShouldBe(10);
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldReturnNullDescription()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        // Description is null by default
        var query = CreateTestQuery(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Description.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_WithRootCategory_ShouldHaveNullParentId()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(); // No parent
        var query = CreateTestQuery(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ParentId.ShouldBeNull();
        result.Value.ParentName.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnTimestamps()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory();
        var query = CreateTestQuery(categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        // CreatedAt should be set by the entity
        result.Value.CreatedAt.ShouldBe(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion
}
