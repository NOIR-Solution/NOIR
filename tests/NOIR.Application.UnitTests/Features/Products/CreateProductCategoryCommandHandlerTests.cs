using NOIR.Application.Features.Products.Commands.CreateProductCategory;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for CreateProductCategoryCommandHandler.
/// Tests category creation scenarios with mocked dependencies.
/// </summary>
public class CreateProductCategoryCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductCategory, Guid>> _categoryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateProductCategoryCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CreateProductCategoryCommandHandlerTests()
    {
        _categoryRepositoryMock = new Mock<IRepository<ProductCategory, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Setup default tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new CreateProductCategoryCommandHandler(
            _categoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static CreateProductCategoryCommand CreateTestCommand(
        string name = "Test Category",
        string slug = "test-category",
        string? description = "Test description",
        string? metaTitle = null,
        string? metaDescription = null,
        string? imageUrl = null,
        int sortOrder = 0,
        Guid? parentId = null)
    {
        return new CreateProductCategoryCommand(
            name,
            slug,
            description,
            metaTitle,
            metaDescription,
            imageUrl,
            sortOrder,
            parentId);
    }

    private static ProductCategory CreateTestCategory(
        string name = "Test Category",
        string slug = "test-category")
    {
        return ProductCategory.Create(name, slug, null, TestTenantId);
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
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory c, CancellationToken _) => c);

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
            x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()),
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
        var parentCategory = CreateTestCategory("Parent Category", "parent-category");
        var command = CreateTestCommand(parentId: parentId);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parentCategory);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory c, CancellationToken _) => c);

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
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory c, CancellationToken _) => c);

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
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory c, CancellationToken _) => c);

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
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory c, CancellationToken _) => c);

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
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCategory);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("NOIR-PRODUCT-001");
        result.Error.Message.Should().Contain("already exists");

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()),
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
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategoryByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-PRODUCT-002");
        result.Error.Message.Should().Contain("not found");

        _categoryRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()),
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
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory c, CancellationToken _) => c);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _categoryRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ProductCategorySlugExistsSpec>(), token),
            Times.Once);
        _categoryRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductCategory>(), token),
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
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory c, CancellationToken _) => c);

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
            x => x.FirstOrDefaultAsync(It.IsAny<ProductCategoryByIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldCreateCategoryWithNullDescription()
    {
        // Arrange
        var command = CreateTestCommand(description: null);

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory c, CancellationToken _) => c);

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
        ProductCategory? capturedCategory = null;

        _categoryRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductCategorySlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductCategory?)null);

        _categoryRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductCategory>(), It.IsAny<CancellationToken>()))
            .Callback<ProductCategory, CancellationToken>((c, _) => capturedCategory = c)
            .ReturnsAsync((ProductCategory c, CancellationToken _) => c);

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
