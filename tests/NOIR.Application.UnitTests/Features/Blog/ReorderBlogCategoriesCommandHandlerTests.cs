using NOIR.Application.Features.Blog.Commands.ReorderCategories;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for ReorderBlogCategoriesCommandHandler.
/// Tests bulk reordering of blog categories with sort order and parent assignment.
/// </summary>
public class ReorderBlogCategoriesCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly ReorderBlogCategoriesCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public ReorderBlogCategoriesCommandHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepository<PostCategory, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new ReorderBlogCategoriesCommandHandler(
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static PostCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category",
        string? description = "Test description",
        Guid? parentId = null)
    {
        return PostCategory.Create(name, slug, description, parentId, TestTenantId);
    }

    private void SetupAllCategoriesReturn(List<PostCategory> categories)
    {
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<CategoriesSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);
    }

    private void SetupCategoriesByIdsReturn(List<PostCategory> categories)
    {
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<CategoriesByIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);
    }

    private void SetupSaveChanges()
    {
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithSingleCategory_ShouldReturnSuccessWithUpdatedList()
    {
        // Arrange
        var category = CreateTestCategory("Category A", "category-a");
        var categories = new List<PostCategory> { category };

        SetupCategoriesByIdsReturn(categories);
        SetupAllCategoriesReturn(categories);
        SetupSaveChanges();

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(category.Id, null, 5)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].Name.Should().Be("Category A");
    }

    [Fact]
    public async Task Handle_WithMultipleCategories_ShouldReturnSuccessWithAllCategories()
    {
        // Arrange
        var category1 = CreateTestCategory("Category A", "category-a");
        var category2 = CreateTestCategory("Category B", "category-b");
        var category3 = CreateTestCategory("Category C", "category-c");
        var categories = new List<PostCategory> { category1, category2, category3 };

        SetupCategoriesByIdsReturn(categories);
        SetupAllCategoriesReturn(categories);
        SetupSaveChanges();

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(category1.Id, null, 0),
            new(category2.Id, null, 1),
            new(category3.Id, null, 2)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithParentAssignment_ShouldUpdateParentAndReturnParentName()
    {
        // Arrange
        var parentCategory = CreateTestCategory("Parent", "parent");
        var childCategory = CreateTestCategory("Child", "child");
        var categories = new List<PostCategory> { parentCategory, childCategory };

        SetupCategoriesByIdsReturn(categories);
        SetupSaveChanges();

        // After update, child has parent set - simulate the state after handler modifies
        SetupAllCategoriesReturn(categories);

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(parentCategory.Id, null, 0),
            new(childCategory.Id, parentCategory.Id, 1)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Verify the child category's parent was set
        childCategory.ParentId.Should().Be(parentCategory.Id);
    }

    [Fact]
    public async Task Handle_WithNullParent_ShouldMakeCategoryTopLevel()
    {
        // Arrange
        var parentCategory = CreateTestCategory("Parent", "parent");
        var childCategory = CreateTestCategory("Child", "child", parentId: parentCategory.Id);
        var categories = new List<PostCategory> { parentCategory, childCategory };

        SetupCategoriesByIdsReturn(categories);
        SetupAllCategoriesReturn(categories);
        SetupSaveChanges();

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(parentCategory.Id, null, 0),
            new(childCategory.Id, null, 1) // Set parent to null
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        childCategory.ParentId.Should().BeNull();
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task Handle_WithInvalidCategoryId_ShouldReturnValidationErrorWithCode010()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        SetupCategoriesByIdsReturn(new List<PostCategory>()); // No categories found

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(invalidId, null, 0)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-BLOG-CATEGORY-010");
        result.Error.Message.Should().Contain(invalidId.ToString());
    }

    [Fact]
    public async Task Handle_WhenCategorySetAsOwnParent_ShouldReturnValidationErrorWithCode011()
    {
        // Arrange
        var category = CreateTestCategory("Category A", "category-a");
        SetupCategoriesByIdsReturn(new List<PostCategory> { category });

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(category.Id, category.Id, 0) // Category is its own parent
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-BLOG-CATEGORY-011");
        result.Error.Message.Should().Contain("cannot be its own parent");
    }

    [Fact]
    public async Task Handle_WithMultipleInvalidIds_ShouldReturnAllInvalidIdsInMessage()
    {
        // Arrange
        var validCategory = CreateTestCategory("Valid", "valid");
        var invalidId1 = Guid.NewGuid();
        var invalidId2 = Guid.NewGuid();

        SetupCategoriesByIdsReturn(new List<PostCategory> { validCategory });

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(validCategory.Id, null, 0),
            new(invalidId1, null, 1),
            new(invalidId2, null, 2)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-CATEGORY-010");
        result.Error.Message.Should().Contain(invalidId1.ToString());
        result.Error.Message.Should().Contain(invalidId2.ToString());
    }

    [Fact]
    public async Task Handle_OnValidationFailure_ShouldNotCallSaveChangesAsync()
    {
        // Arrange
        var invalidId = Guid.NewGuid();
        SetupCategoriesByIdsReturn(new List<PostCategory>());

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(invalidId, null, 0)
        });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_OnSelfParentFailure_ShouldNotCallSaveChangesAsync()
    {
        // Arrange
        var category = CreateTestCategory("Category A", "category-a");
        SetupCategoriesByIdsReturn(new List<PostCategory> { category });

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(category.Id, category.Id, 0)
        });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Verification Scenarios

    [Fact]
    public async Task Handle_OnSuccess_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var category = CreateTestCategory("Category A", "category-a");
        var categories = new List<PostCategory> { category };

        SetupCategoriesByIdsReturn(categories);
        SetupAllCategoriesReturn(categories);
        SetupSaveChanges();

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(category.Id, null, 3)
        });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithReorderItems_ShouldUpdateSortOrderAndParentOnEntities()
    {
        // Arrange
        var parentCategory = CreateTestCategory("Parent", "parent");
        var childCategory = CreateTestCategory("Child", "child");
        var categories = new List<PostCategory> { parentCategory, childCategory };

        SetupCategoriesByIdsReturn(categories);
        SetupAllCategoriesReturn(categories);
        SetupSaveChanges();

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(parentCategory.Id, null, 10),
            new(childCategory.Id, parentCategory.Id, 20)
        });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        parentCategory.SortOrder.Should().Be(10);
        parentCategory.ParentId.Should().BeNull();
        childCategory.SortOrder.Should().Be(20);
        childCategory.ParentId.Should().Be(parentCategory.Id);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithEmptyList_ShouldSucceedWithNoChanges()
    {
        // Arrange
        SetupCategoriesByIdsReturn(new List<PostCategory>());
        SetupAllCategoriesReturn(new List<PostCategory>());
        SetupSaveChanges();

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToAllCalls()
    {
        // Arrange
        var category = CreateTestCategory("Token Test", "token-test");
        var categories = new List<PostCategory> { category };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<CategoriesByIdsForUpdateSpec>(), token))
            .ReturnsAsync(categories);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);
        _categoryRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<CategoriesSpec>(), token))
            .ReturnsAsync(categories);

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(category.Id, null, 0)
        });

        // Act
        await _handler.Handle(command, token);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CategoriesByIdsForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
        _categoryRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CategoriesSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtosWithCorrectParentNames()
    {
        // Arrange
        var parentCategory = CreateTestCategory("Technology", "technology");
        var childCategory = CreateTestCategory("Programming", "programming");
        var categories = new List<PostCategory> { parentCategory, childCategory };

        SetupCategoriesByIdsReturn(categories);
        SetupSaveChanges();

        // After save, return categories with updated state
        SetupAllCategoriesReturn(categories);

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(parentCategory.Id, null, 0),
            new(childCategory.Id, parentCategory.Id, 1)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var childDto = result.Value.FirstOrDefault(c => c.Id == childCategory.Id);
        childDto.Should().NotBeNull();
        childDto!.ParentId.Should().Be(parentCategory.Id);
        childDto.ParentName.Should().Be("Technology");
    }

    [Fact]
    public async Task Handle_ShouldReturnDtosWithCorrectChildCounts()
    {
        // Arrange
        var parentCategory = CreateTestCategory("Parent", "parent");
        var child1 = CreateTestCategory("Child 1", "child-1");
        var child2 = CreateTestCategory("Child 2", "child-2");
        var categories = new List<PostCategory> { parentCategory, child1, child2 };

        SetupCategoriesByIdsReturn(categories);
        SetupSaveChanges();
        SetupAllCategoriesReturn(categories);

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(parentCategory.Id, null, 0),
            new(child1.Id, parentCategory.Id, 1),
            new(child2.Id, parentCategory.Id, 2)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var parentDto = result.Value.FirstOrDefault(c => c.Id == parentCategory.Id);
        parentDto.Should().NotBeNull();
        parentDto!.ChildCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtosWithPostCount()
    {
        // Arrange
        var category = CreateTestCategory("Popular", "popular");
        category.IncrementPostCount();
        category.IncrementPostCount();
        category.IncrementPostCount();

        var categories = new List<PostCategory> { category };

        SetupCategoriesByIdsReturn(categories);
        SetupSaveChanges();
        SetupAllCategoriesReturn(categories);

        var command = new ReorderBlogCategoriesCommand(new List<BlogCategorySortOrderItem>
        {
            new(category.Id, null, 0)
        });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.First();
        dto.PostCount.Should().Be(3);
    }

    #endregion
}
