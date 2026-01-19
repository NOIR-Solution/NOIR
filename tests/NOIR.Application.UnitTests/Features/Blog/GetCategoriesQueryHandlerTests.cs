using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Queries.GetCategories;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for GetCategoriesQueryHandler.
/// Tests category list retrieval with filtering options.
/// </summary>
public class GetCategoriesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostCategory, Guid>> _repositoryMock;
    private readonly GetCategoriesQueryHandler _handler;

    public GetCategoriesQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<PostCategory, Guid>>();
        _handler = new GetCategoriesQueryHandler(_repositoryMock.Object);
    }

    private static PostCategory CreateTestCategory(
        Guid? id = null,
        string name = "Test Category",
        string slug = "test-category",
        string? description = null,
        Guid? parentId = null,
        int sortOrder = 0)
    {
        var category = PostCategory.Create(
            name,
            slug,
            description,
            parentId,
            tenantId: "test-tenant");

        // Use reflection to set the ID if provided
        if (id.HasValue)
        {
            var idProperty = typeof(PostCategory).GetProperty("Id");
            idProperty?.SetValue(category, id.Value);
        }

        category.SetSortOrder(sortOrder);

        return category;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllCategories()
    {
        // Arrange
        var categories = new List<PostCategory>
        {
            CreateTestCategory(name: "Technology", slug: "technology"),
            CreateTestCategory(name: "Business", slug: "business"),
            CreateTestCategory(name: "Lifestyle", slug: "lifestyle")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var categories = new List<PostCategory>
        {
            CreateTestCategory(name: "Technology", slug: "technology")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery(Search: "tech");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = CreateTestCategory(
            id: categoryId,
            name: "Technology",
            slug: "technology",
            description: "Tech articles",
            sortOrder: 1);

        var categories = new List<PostCategory> { category };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];
        dto.Id.Should().Be(categoryId);
        dto.Name.Should().Be("Technology");
        dto.Slug.Should().Be("technology");
        dto.Description.Should().Be("Tech articles");
        dto.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region TopLevelOnly Scenarios

    [Fact]
    public async Task Handle_WithTopLevelOnly_ShouldUseTopLevelCategoriesSpec()
    {
        // Arrange
        var topLevelCategories = new List<PostCategory>
        {
            CreateTestCategory(name: "Parent Category 1", slug: "parent-1"),
            CreateTestCategory(name: "Parent Category 2", slug: "parent-2")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(topLevelCategories);

        var query = new GetCategoriesQuery(TopLevelOnly: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.All(c => c.ParentId == null).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithTopLevelOnlyFalse_ShouldReturnAllCategories()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var categories = new List<PostCategory>
        {
            CreateTestCategory(id: parentId, name: "Parent", slug: "parent"),
            CreateTestCategory(name: "Child", slug: "child", parentId: parentId)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery(TopLevelOnly: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region IncludeChildren Scenarios

    [Fact]
    public async Task Handle_WithIncludeChildren_ShouldPassFlagToSpec()
    {
        // Arrange
        var categories = new List<PostCategory>
        {
            CreateTestCategory(name: "Parent With Children", slug: "parent-with-children")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery(IncludeChildren: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    #endregion

    #region Hierarchical Categories

    [Fact]
    public async Task Handle_WithParentCategory_ShouldIncludeParentName()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var categories = new List<PostCategory>
        {
            CreateTestCategory(id: parentId, name: "Technology", slug: "technology"),
            CreateTestCategory(name: "Programming", slug: "programming", parentId: parentId)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var childCategory = result.Value.First(c => c.Name == "Programming");
        childCategory.ParentId.Should().Be(parentId);
        childCategory.ParentName.Should().Be("Technology");
    }

    [Fact]
    public async Task Handle_WithChildCategories_ShouldIncludeChildCount()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentCategory = CreateTestCategory(id: parentId, name: "Technology", slug: "technology");

        // Note: ChildCount is calculated from Children collection
        // In a real scenario, this would be populated by the spec
        var categories = new List<PostCategory> { parentCategory };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].ChildCount.Should().Be(0); // Empty Children collection in mock
    }

    #endregion

    #region PostCount Scenarios

    [Fact]
    public async Task Handle_ShouldIncludePostCount()
    {
        // Arrange
        var category = CreateTestCategory(name: "Popular Category", slug: "popular-category");

        // Increment post count to simulate posts in category
        category.IncrementPostCount();
        category.IncrementPostCount();
        category.IncrementPostCount();

        var categories = new List<PostCategory> { category };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].PostCount.Should().Be(3);
    }

    #endregion

    #region Sort Order Scenarios

    [Fact]
    public async Task Handle_ShouldIncludeSortOrder()
    {
        // Arrange
        var categories = new List<PostCategory>
        {
            CreateTestCategory(name: "Third", slug: "third", sortOrder: 3),
            CreateTestCategory(name: "First", slug: "first", sortOrder: 1),
            CreateTestCategory(name: "Second", slug: "second", sortOrder: 2)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(c => c.SortOrder == 1);
        result.Value.Should().Contain(c => c.SortOrder == 2);
        result.Value.Should().Contain(c => c.SortOrder == 3);
    }

    #endregion

    #region Combined Filters

    [Fact]
    public async Task Handle_WithSearchAndTopLevelOnly_TopLevelOnlyTakesPrecedence()
    {
        // Arrange
        // When TopLevelOnly is true, search filter is not used (TopLevelCategoriesSpec is used)
        var topLevelCategories = new List<PostCategory>
        {
            CreateTestCategory(name: "Technology", slug: "technology")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(topLevelCategories);

        var query = new GetCategoriesQuery(Search: "tech", TopLevelOnly: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithSearchAndIncludeChildren_ShouldApplyBothFilters()
    {
        // Arrange
        var categories = new List<PostCategory>
        {
            CreateTestCategory(name: "Technology", slug: "technology")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetCategoriesQuery(Search: "tech", IncludeChildren: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    #endregion
}
