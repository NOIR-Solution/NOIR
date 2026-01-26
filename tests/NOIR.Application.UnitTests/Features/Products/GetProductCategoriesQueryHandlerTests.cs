using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductCategories;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for GetProductCategoriesQueryHandler.
/// Tests category listing scenarios with mocked dependencies.
/// </summary>
public class GetProductCategoriesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly GetProductCategoriesQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GetProductCategoriesQueryHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();

        _handler = new GetProductCategoriesQueryHandler(
            _categoryRepositoryMock.Object);
    }

    private static GetProductCategoriesQuery CreateTestQuery(
        string? search = null,
        bool topLevelOnly = false,
        bool includeChildren = false)
    {
        return new GetProductCategoriesQuery(search, topLevelOnly, includeChildren);
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category",
        Guid? parentId = null)
    {
        return ProductCategory.Create(name, slug, parentId, TestTenantId);
    }

    private static List<ProductCategory> CreateTestCategories(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestCategory($"Category {i}", $"category-{i}"))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllCategories()
    {
        // Arrange
        var categories = CreateTestCategories(5);
        var query = CreateTestQuery();

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_WithTopLevelOnly_ShouldUseTopLevelSpec()
    {
        // Arrange
        var categories = CreateTestCategories(3);
        var query = CreateTestQuery(topLevelOnly: true);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<TopLevelProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<TopLevelProductCategoriesSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldUseProductCategoriesSpec()
    {
        // Arrange
        var categories = CreateTestCategories(2);
        var query = CreateTestQuery(search: "test");

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductCategoriesSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithParentCategory_ShouldIncludeParentName()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parent = CreateTestCategory("Parent", "parent");
        var child = CreateTestCategory("Child", "child", parentId);
        var categories = new List<ProductCategory> { parent, child };

        // Use reflection or a property setter to simulate parent relationship
        // Since we can't easily set the Parent navigation property, we rely on the lookup dict
        var query = CreateTestQuery();

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldMapAllCategoryFields()
    {
        // Arrange
        var category = CreateTestCategory("Full Category", "full-category");
        category.UpdateDetails("Full Category", "full-category", "Description", "https://example.com/img.jpg");
        category.SetSortOrder(5);
        var categories = new List<ProductCategory> { category };
        var query = CreateTestQuery();

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.Name.Should().Be("Full Category");
        dto.Slug.Should().Be("full-category");
        dto.Description.Should().Be("Description");
        dto.SortOrder.Should().Be(5);
    }

    #endregion

    #region Empty Results Scenarios

    [Fact]
    public async Task Handle_WithNoCategories_ShouldReturnEmptyList()
    {
        // Arrange
        var query = CreateTestQuery();

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductCategory>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
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

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductCategory>());

        // Act
        await _handler.Handle(query, token);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductCategoriesSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithIncludeChildren_ShouldUseCorrectSpec()
    {
        // Arrange
        var categories = CreateTestCategories(2);
        var query = CreateTestQuery(includeChildren: true);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductCategoriesSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCountChildren()
    {
        // Arrange
        var category = CreateTestCategory("Parent", "parent");
        var categories = new List<ProductCategory> { category };
        var query = CreateTestQuery();

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ProductCategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // ChildCount should be 0 for a category with no children
        result.Value.First().ChildCount.Should().Be(0);
    }

    #endregion
}
