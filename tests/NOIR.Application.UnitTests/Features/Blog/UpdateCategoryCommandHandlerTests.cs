using NOIR.Application.Features.Blog.Commands.UpdateCategory;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for UpdateCategoryCommandHandler.
/// Tests category update scenarios with mocked dependencies.
/// </summary>
public class UpdateCategoryCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UpdateCategoryCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public UpdateCategoryCommandHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepository<PostCategory, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Setup default tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new UpdateCategoryCommandHandler(
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static UpdateCategoryCommand CreateTestCommand(
        Guid? id = null,
        string name = "Updated Category",
        string slug = "updated-category",
        string? description = "Updated description",
        string? metaTitle = null,
        string? metaDescription = null,
        string? imageUrl = null,
        int sortOrder = 0,
        Guid? parentId = null)
    {
        return new UpdateCategoryCommand(
            id ?? Guid.NewGuid(),
            name,
            slug,
            description,
            metaTitle,
            metaDescription,
            imageUrl,
            sortOrder,
            parentId);
    }

    private static PostCategory CreateTestCategory(
        Guid? id = null,
        string name = "Test Category",
        string slug = "test-category",
        string? description = "Test description")
    {
        var category = PostCategory.Create(
            name,
            slug,
            description,
            null,
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
    public async Task Handle_WithValidData_ShouldUpdateCategory()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId, "Original Name", "original-slug");
        var command = CreateTestCommand(
            id: categoryId,
            name: "Updated Name",
            slug: "updated-slug",
            description: "Updated description");

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Updated Name");
        result.Value.Slug.Should().Be("updated-slug");
        result.Value.Description.Should().Be("Updated description");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSameSlug_ShouldNotCheckSlugUniqueness()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId, "Original Name", "same-slug");
        var command = CreateTestCommand(
            id: categoryId,
            slug: "same-slug"); // Same slug

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should not check for slug uniqueness when slug hasn't changed
        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CategorySlugExistsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithParentCategory_ShouldUpdateParent()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId, "Child Category", "child-category");
        var parentCategory = CreateTestCategory(parentId, "Parent Category", "parent-category");
        var command = CreateTestCommand(
            id: categoryId,
            parentId: parentId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentCategory);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().Be(parentId);
        result.Value.ParentName.Should().Be(parentCategory.Name);
    }

    [Fact]
    public async Task Handle_WithSeoFields_ShouldUpdateSeoMetadata()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(
            id: categoryId,
            metaTitle: "New SEO Title",
            metaDescription: "New SEO Description");

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MetaTitle.Should().Be("New SEO Title");
        result.Value.MetaDescription.Should().Be("New SEO Description");
    }

    [Fact]
    public async Task Handle_WithImageUrl_ShouldUpdateImage()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(
            id: categoryId,
            imageUrl: "https://example.com/new-image.jpg");

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImageUrl.Should().Be("https://example.com/new-image.jpg");
    }

    [Fact]
    public async Task Handle_WithSortOrder_ShouldUpdateSortOrder()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(
            id: categoryId,
            sortOrder: 10);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SortOrder.Should().Be(10);
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

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenParentCategoryNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var nonExistentParentId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(
            id: categoryId,
            parentId: nonExistentParentId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-006");
        result.Error.Message.Should().Contain("Parent category");
        result.Error.Message.Should().Contain("not found");
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId, "Original Name", "original-slug");
        var conflictingCategory = CreateTestCategory(Guid.NewGuid(), "Other Category", "new-slug");
        var command = CreateTestCommand(
            id: categoryId,
            slug: "new-slug"); // This slug already exists

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictingCategory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-005");
        result.Error.Message.Should().Contain("already exists");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WhenSelfReferencing_ShouldReturnValidationError()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(
            id: categoryId,
            parentId: categoryId); // Self-referencing

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-008");
        result.Error.Message.Should().Contain("cannot be its own parent");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
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
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CategoryByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RemovingParent_ShouldUpdateToNoParent()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId);
        var command = CreateTestCommand(
            id: categoryId,
            parentId: null); // Removing parent

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ParentId.Should().BeNull();
        result.Value.ParentName.Should().BeNull();

        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CategoryByIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SlugWithMixedCase_ShouldCompareAsLowercase()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var existingCategory = CreateTestCategory(categoryId, "Original Name", "test-slug");
        var command = CreateTestCommand(
            id: categoryId,
            slug: "TEST-SLUG"); // Same slug but uppercase

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategoryByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should not check for slug uniqueness since it's the same when lowercased
        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CategorySlugExistsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
