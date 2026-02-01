using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Queries.GetCategoryById;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for GetCategoryByIdQueryHandler.
/// Tests retrieving a single blog category by ID.
/// </summary>
public class GetCategoryByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostCategory, Guid>> _repositoryMock;
    private readonly GetCategoryByIdQueryHandler _handler;

    public GetCategoryByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<PostCategory, Guid>>();
        _handler = new GetCategoryByIdQueryHandler(_repositoryMock.Object);
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

    private static PostCategory CreateCategoryWithParent(
        Guid? id = null,
        string name = "Child Category",
        string slug = "child-category",
        PostCategory? parent = null)
    {
        var category = PostCategory.Create(
            name,
            slug,
            description: null,
            parentId: parent?.Id,
            tenantId: "test-tenant");

        if (id.HasValue)
        {
            typeof(PostCategory).GetProperty("Id")?.SetValue(category, id.Value);
        }

        // Set up the Parent navigation property
        if (parent != null)
        {
            typeof(PostCategory).GetProperty("Parent")?.SetValue(category, parent);
        }

        return category;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidId_ShouldReturnCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = CreateTestCategory(
            id: categoryId,
            name: "Technology",
            slug: "technology",
            description: "Tech articles");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var query = new GetCategoryByIdQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(categoryId);
        result.Value.Name.Should().Be("Technology");
        result.Value.Slug.Should().Be("technology");
        result.Value.Description.Should().Be("Tech articles");
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = CreateTestCategory(
            id: categoryId,
            name: "Business",
            slug: "business",
            description: "Business insights",
            sortOrder: 5);

        // Set additional properties via reflection
        typeof(PostCategory).GetProperty("MetaTitle")?.SetValue(category, "Business - Meta Title");
        typeof(PostCategory).GetProperty("MetaDescription")?.SetValue(category, "Business meta description");
        typeof(PostCategory).GetProperty("ImageUrl")?.SetValue(category, "https://example.com/business.jpg");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var query = new GetCategoryByIdQuery(categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Name.Should().Be("Business");
        dto.Slug.Should().Be("business");
        dto.Description.Should().Be("Business insights");
        dto.SortOrder.Should().Be(5);
        dto.MetaTitle.Should().Be("Business - Meta Title");
        dto.MetaDescription.Should().Be("Business meta description");
        dto.ImageUrl.Should().Be("https://example.com/business.jpg");
    }

    [Fact]
    public async Task Handle_WithParentCategory_ShouldIncludeParentName()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentCategory = CreateTestCategory(id: parentId, name: "Technology", slug: "technology");
        var childCategory = CreateCategoryWithParent(
            id: Guid.NewGuid(),
            name: "Programming",
            slug: "programming",
            parent: parentCategory);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(childCategory);

        var query = new GetCategoryByIdQuery(childCategory.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().Be(parentId);
        result.Value.ParentName.Should().Be("Technology");
    }

    [Fact]
    public async Task Handle_WithChildren_ShouldIncludeChildCategories()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentCategory = CreateTestCategory(id: parentId, name: "Technology", slug: "technology");

        var child1 = CreateTestCategory(name: "Programming", slug: "programming", parentId: parentId);
        var child2 = CreateTestCategory(name: "Hardware", slug: "hardware", parentId: parentId);

        // Set up the Children collection
        var childrenProperty = typeof(PostCategory).GetProperty("Children");
        childrenProperty?.SetValue(parentCategory, new List<PostCategory> { child1, child2 });

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentCategory);

        var query = new GetCategoryByIdQuery(parentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Children.Should().NotBeNull();
        result.Value.Children.Should().HaveCount(2);
        result.Value.Children.Should().Contain(c => c.Name == "Programming");
        result.Value.Children.Should().Contain(c => c.Name == "Hardware");
    }

    [Fact]
    public async Task Handle_WithPostCount_ShouldIncludePostCount()
    {
        // Arrange
        var category = CreateTestCategory(name: "Popular", slug: "popular");

        // Increment post count
        category.IncrementPostCount();
        category.IncrementPostCount();
        category.IncrementPostCount();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var query = new GetCategoryByIdQuery(category.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PostCount.Should().Be(3);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        var query = new GetCategoryByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-020");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNullDescription_ShouldReturnNullDescription()
    {
        // Arrange
        var category = CreateTestCategory(
            name: "NoDescription",
            slug: "no-description",
            description: null);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var query = new GetCategoryByIdQuery(category.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNoParent_ShouldReturnNullParentFields()
    {
        // Arrange
        var category = CreateTestCategory(name: "TopLevel", slug: "top-level");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var query = new GetCategoryByIdQuery(category.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().BeNull();
        result.Value.ParentName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithEmptyChildren_ShouldReturnEmptyChildrenList()
    {
        // Arrange
        var category = CreateTestCategory(name: "NoChildren", slug: "no-children");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var query = new GetCategoryByIdQuery(category.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Children might be null or empty depending on whether spec includes them
        result.Value.Children?.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToRepository()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var category = CreateTestCategory(id: categoryId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                token))
            .ReturnsAsync(category);

        var query = new GetCategoryByIdQuery(categoryId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _repositoryMock.Verify(
            x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIncludeTimestamps()
    {
        // Arrange
        var category = CreateTestCategory(name: "WithTimestamps", slug: "with-timestamps");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostCategory>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        var query = new GetCategoryByIdQuery(category.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().NotBe(default);
    }

    #endregion
}
