using NOIR.Application.Features.Blog.Commands.DeleteCategory;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for DeleteCategoryCommandHandler.
/// Tests category deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteCategoryCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteCategoryCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public DeleteCategoryCommandHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepository<PostCategory, Guid>>();
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteCategoryCommandHandler(
            _categoryRepositoryMock.Object,
            _postRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static DeleteCategoryCommand CreateTestCommand(
        Guid? id = null,
        string? categoryName = null)
    {
        return new DeleteCategoryCommand(
            id ?? Guid.NewGuid(),
            categoryName);
    }

    private static PostCategory CreateTestCategory(
        Guid? id = null,
        string name = "Test Category",
        string slug = "test-category",
        string? description = "Test description",
        Guid? parentId = null)
    {
        var category = PostCategory.Create(
            name,
            slug,
            description,
            parentId,
            TestTenantId);

        // Use reflection to set the ID since it's set in Create
        if (id.HasValue)
        {
            typeof(PostCategory).GetProperty("Id")!.SetValue(category, id.Value);
        }

        return category;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCategory_ShouldDeleteCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(id: categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>()); // No children

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoryHasPostsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>()); // No posts

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _categoryRepositoryMock.Verify(
            x => x.Remove(existingCategory),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCategoryName_ShouldDeleteCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var categoryName = "Tech Articles";
        var existingCategory = CreateTestCategory(categoryId, categoryName);
        var command = CreateTestCommand(id: categoryId, categoryName: categoryName);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoryHasPostsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var command = CreateTestCommand(id: nonExistentId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-007");
        result.Error.Message.Should().Contain("not found");

        _categoryRepositoryMock.Verify(
            x => x.Remove(It.IsAny<PostCategory>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenCategoryHasChildren_ShouldReturnConflict()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var parentCategory = CreateTestCategory(parentId, "Parent Category");
        var childCategory = CreateTestCategory(childId, "Child Category", parentId: parentId);
        var command = CreateTestCommand(id: parentId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory> { childCategory }); // Has children

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-009");
        result.Error.Message.Should().Contain("child categories");

        _categoryRepositoryMock.Verify(
            x => x.Remove(It.IsAny<PostCategory>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCategoryHasPosts_ShouldReturnConflict()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(id: categoryId);

        // Create a real Post instance instead of mocking (Post has no parameterless constructor)
        var testPost = Post.Create("Test Post", "test-post", Guid.NewGuid(), TestTenantId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>()); // No children

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoryHasPostsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post> { testPost }); // Has posts

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-010");
        result.Error.Message.Should().Contain("has posts");

        _categoryRepositoryMock.Verify(
            x => x.Remove(It.IsAny<PostCategory>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCategoryHasChildrenAndPosts_ShouldReturnConflictForChildren()
    {
        // Arrange - Children check happens first
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var parentCategory = CreateTestCategory(parentId, "Parent Category");
        var childCategory = CreateTestCategory(childId, "Child Category", parentId: parentId);
        var command = CreateTestCommand(id: parentId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory> { childCategory }); // Has children

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should fail on children check first
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-009");

        // Posts should not be checked since children check fails first
        _postRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CategoryHasPostsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(id: categoryId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoryHasPostsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CategoryByIdForUpdateSpec>(), token),
            Times.Once);
        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CategoriesSpec>(), token),
            Times.Once);
        _postRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CategoryHasPostsSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SoftDeleteCategory_ShouldCallRemove()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(id: categoryId);
        PostCategory? removedCategory = null;

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory>());

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoryHasPostsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _categoryRepositoryMock
            .Setup(x => x.Remove(It.IsAny<PostCategory>()))
            .Callback<PostCategory>(c => removedCategory = c);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        removedCategory.Should().NotBeNull();
        removedCategory.Should().BeSameAs(existingCategory);
    }

    [Fact]
    public async Task Handle_CategoryWithNoChildrenOrPosts_ShouldSucceed()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId, "Empty Category");
        var command = CreateTestCommand(id: categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Return other categories that are NOT children of this one
        var unrelatedCategory = CreateTestCategory(Guid.NewGuid(), "Unrelated");
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory> { unrelatedCategory }); // No children of target

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoryHasPostsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>()); // No posts

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenMultipleCategoriesExist_ShouldOnlyCheckChildrenOfTargetCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId, "Target Category");
        var otherCategory = CreateTestCategory(otherId, "Other Category");
        var command = CreateTestCommand(id: categoryId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Return categories, but none are children of the target
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoriesSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostCategory> { existingCategory, otherCategory });

        _postRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<CategoryHasPostsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
