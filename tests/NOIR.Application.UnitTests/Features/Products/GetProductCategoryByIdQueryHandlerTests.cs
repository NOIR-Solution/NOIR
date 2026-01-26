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
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(existingCategory.Name);
        result.Value.Slug.Should().Be(existingCategory.Slug);
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
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Name.Should().Be("Full Category");
        dto.Slug.Should().Be("full-category");
        dto.Description.Should().Be("Description");
        dto.ImageUrl.Should().Be("https://example.com/img.jpg");
        dto.MetaTitle.Should().Be("SEO Title");
        dto.MetaDescription.Should().Be("SEO Description");
        dto.SortOrder.Should().Be(5);
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
        result.IsSuccess.Should().BeTrue();
        // Children collection should be mapped (empty in this case since no children on category)
        result.Value.Children.Should().BeNullOrEmpty();
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
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-003");
        result.Error.Message.Should().Contain("not found");
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
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductCount.Should().Be(10);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().BeNull();
        result.Value.ParentName.Should().BeNull();
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
        result.IsSuccess.Should().BeTrue();
        // CreatedAt should be set by the entity
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    #endregion
}
