namespace NOIR.Application.UnitTests.Features.Brands;

/// <summary>
/// Unit tests for UpdateBrandCommandHandler.
/// Tests brand update scenarios with mocked dependencies.
/// </summary>
public class UpdateBrandCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Brand, Guid>> _brandRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IEntityUpdateHubContext> _entityUpdateHubMock = new();
    private readonly UpdateBrandCommandHandler _handler;

    public UpdateBrandCommandHandlerTests()
    {
        _brandRepositoryMock = new Mock<IRepository<Brand, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateBrandCommandHandler(
            _brandRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _entityUpdateHubMock.Object);
    }

    private static Brand CreateTestBrand(
        Guid? id = null,
        string name = "Test Brand",
        string slug = "test-brand")
    {
        var brandId = id ?? Guid.NewGuid();
        return Brand.Create(name, slug, "tenant-123");
    }

    private static UpdateBrandCommand CreateValidCommand(
        Guid? id = null,
        string name = "Updated Brand",
        string slug = "updated-brand",
        string? description = null,
        string? website = null,
        string? logoUrl = null,
        string? bannerUrl = null,
        string? metaTitle = null,
        string? metaDescription = null,
        bool isActive = true,
        bool isFeatured = false,
        int sortOrder = 0)
    {
        return new UpdateBrandCommand(
            id ?? Guid.NewGuid(),
            name,
            slug,
            description,
            website,
            logoUrl,
            bannerUrl,
            metaTitle,
            metaDescription,
            isActive,
            isFeatured,
            sortOrder);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = CreateTestBrand();

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateValidCommand(id: brandId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Updated Brand");
        result.Value.Slug.ShouldBe("updated-brand");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSameSlug_ShouldNotCheckForDuplicate()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = Brand.Create("Test Brand", "same-slug", "tenant-123");

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        var command = CreateValidCommand(id: brandId, slug: "same-slug");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);

        _brandRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<BrandSlugExistsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithAllFields_ShouldUpdateAllProperties()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = CreateTestBrand();

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        var command = new UpdateBrandCommand(
            brandId,
            "New Name",
            "new-slug",
            "New Description",
            "https://newwebsite.com",
            "https://cdn.test.com/logo.png",
            "https://cdn.test.com/banner.png",
            "New Meta Title",
            "New Meta Description",
            IsActive: true,
            IsFeatured: true,
            SortOrder: 5);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("New Name");
        result.Value.Slug.ShouldBe("new-slug");
        result.Value.Description.ShouldBe("New Description");
        result.Value.Website.ShouldBe("https://newwebsite.com");
        result.Value.LogoUrl.ShouldBe("https://cdn.test.com/logo.png");
        result.Value.BannerUrl.ShouldBe("https://cdn.test.com/banner.png");
        result.Value.MetaTitle.ShouldBe("New Meta Title");
        result.Value.MetaDescription.ShouldBe("New Meta Description");
        result.Value.IsActive.ShouldBe(true);
        result.Value.IsFeatured.ShouldBe(true);
        result.Value.SortOrder.ShouldBe(5);
    }

    [Fact]
    public async Task Handle_ChangingSlugToUnique_ShouldSucceed()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = Brand.Create("Test Brand", "old-slug", "tenant-123");

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        var command = CreateValidCommand(id: brandId, slug: "new-unique-slug");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Slug.ShouldBe("new-unique-slug");
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenBrandNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var brandId = Guid.NewGuid();

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        var command = CreateValidCommand(id: brandId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe(ErrorCodes.Brand.NotFound);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenNewSlugExistsForDifferentBrand_ShouldReturnConflict()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = Brand.Create("Test Brand", "old-slug", "tenant-123");
        var conflictingBrand = Brand.Create("Other Brand", "conflicting-slug", "tenant-123");

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictingBrand);

        var command = CreateValidCommand(id: brandId, slug: "conflicting-slug");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe(ErrorCodes.Brand.DuplicateSlug);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = CreateTestBrand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                token))
            .ReturnsAsync(existingBrand);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = CreateValidCommand(id: brandId, slug: "test-brand");

        // Act
        await _handler.Handle(command, token);

        // Assert
        _brandRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<BrandByIdForUpdateSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
