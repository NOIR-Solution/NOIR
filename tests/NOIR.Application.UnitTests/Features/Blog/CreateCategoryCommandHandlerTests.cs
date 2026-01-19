using NOIR.Application.Features.Blog.Commands.CreateCategory;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for CreateCategoryCommandHandler.
/// Tests category creation scenarios with mocked dependencies.
/// </summary>
public class CreateCategoryCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateCategoryCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CreateCategoryCommandHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepository<PostCategory, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Setup default tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new CreateCategoryCommandHandler(
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static CreateCategoryCommand CreateTestCommand(
        string name = "Test Category",
        string slug = "test-category",
        string? description = "Test description",
        string? metaTitle = null,
        string? metaDescription = null,
        string? imageUrl = null,
        int sortOrder = 0,
        Guid? parentId = null)
    {
        return new CreateCategoryCommand(
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
        return PostCategory.Create(
            name,
            slug,
            description,
            null,
            TestTenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateCategory()
    {
        // Arrange
        var command = CreateTestCommand();

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory c, CancellationToken _) => c);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(command.Name);
        result.Value.Slug.Should().Be(command.Slug.ToLowerInvariant());
        result.Value.Description.Should().Be(command.Description);
        result.Value.SortOrder.Should().Be(command.SortOrder);

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithParentCategory_ShouldCreateChildCategory()
    {
        // Arrange
        var parentId = Guid.NewGuid();
        var parentCategory = CreateTestCategory(parentId, "Parent Category", "parent-category");
        var command = CreateTestCommand(parentId: parentId);

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

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory c, CancellationToken _) => c);

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
    public async Task Handle_WithSeoFields_ShouldSetSeoMetadata()
    {
        // Arrange
        var command = CreateTestCommand(
            metaTitle: "SEO Title",
            metaDescription: "SEO Description");

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory c, CancellationToken _) => c);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MetaTitle.Should().Be("SEO Title");
        result.Value.MetaDescription.Should().Be("SEO Description");
    }

    [Fact]
    public async Task Handle_WithImageUrl_ShouldSetImage()
    {
        // Arrange
        var command = CreateTestCommand(imageUrl: "https://example.com/image.jpg");

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory c, CancellationToken _) => c);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ImageUrl.Should().Be("https://example.com/image.jpg");
    }

    [Fact]
    public async Task Handle_WithSortOrder_ShouldSetSortOrder()
    {
        // Arrange
        var command = CreateTestCommand(sortOrder: 5);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory c, CancellationToken _) => c);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SortOrder.Should().Be(5);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var existingCategory = CreateTestCategory();
        var command = CreateTestCommand();

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-005");
        result.Error.Message.Should().Contain("already exists");

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenParentCategoryNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentParentId = Guid.NewGuid();
        var command = CreateTestCommand(parentId: nonExistentParentId);

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

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var command = CreateTestCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory c, CancellationToken _) => c);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CategorySlugExistsSpec>(), token),
            Times.Once);
        _categoryRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PostCategory>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutParentId_ShouldNotQueryParentCategory()
    {
        // Arrange
        var command = CreateTestCommand(parentId: null);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory c, CancellationToken _) => c);

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
    public async Task Handle_WithNullDescription_ShouldCreateCategoryWithNullDescription()
    {
        // Arrange
        var command = CreateTestCommand(description: null);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory c, CancellationToken _) => c);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        var command = CreateTestCommand();
        PostCategory? capturedCategory = null;

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostCategory>(), It.IsAny<CancellationToken>()))
            .Callback<PostCategory, CancellationToken>((c, _) => capturedCategory = c)
            .ReturnsAsync((PostCategory c, CancellationToken _) => c);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedCategory.Should().NotBeNull();
        capturedCategory!.TenantId.Should().Be(TestTenantId);
    }

    #endregion
}
