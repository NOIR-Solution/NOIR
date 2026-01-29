namespace NOIR.Application.UnitTests.Features.Brands;

/// <summary>
/// Unit tests for CreateBrandCommandHandler.
/// Tests brand creation scenarios with mocked dependencies.
/// </summary>
public class CreateBrandCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Brand, Guid>> _brandRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateBrandCommandHandler _handler;

    public CreateBrandCommandHandlerTests()
    {
        _brandRepositoryMock = new Mock<IRepository<Brand, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-123");

        _handler = new CreateBrandCommandHandler(
            _brandRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static CreateBrandCommand CreateValidCommand(
        string name = "Test Brand",
        string slug = "test-brand",
        string? logoUrl = null,
        string? bannerUrl = null,
        string? description = null,
        string? website = null,
        string? metaTitle = null,
        string? metaDescription = null,
        bool isFeatured = false)
    {
        return new CreateBrandCommand(
            name,
            slug,
            logoUrl,
            bannerUrl,
            description,
            website,
            metaTitle,
            metaDescription,
            isFeatured);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var command = CreateValidCommand();

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        _brandRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand brand, CancellationToken _) => brand);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Test Brand");
        result.Value.Slug.Should().Be("test-brand");

        _brandRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithDescriptionAndWebsite_ShouldUpdateDetails()
    {
        // Arrange
        var command = CreateValidCommand(
            description: "A test brand description",
            website: "https://testbrand.com");

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        _brandRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand brand, CancellationToken _) => brand);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("A test brand description");
        result.Value.Website.Should().Be("https://testbrand.com");
    }

    [Fact]
    public async Task Handle_WithBrandingAssets_ShouldUpdateBranding()
    {
        // Arrange
        var command = CreateValidCommand(
            logoUrl: "https://cdn.test.com/logo.png",
            bannerUrl: "https://cdn.test.com/banner.png");

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        _brandRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand brand, CancellationToken _) => brand);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LogoUrl.Should().Be("https://cdn.test.com/logo.png");
        result.Value.BannerUrl.Should().Be("https://cdn.test.com/banner.png");
    }

    [Fact]
    public async Task Handle_WithSeoMetadata_ShouldUpdateSeo()
    {
        // Arrange
        var command = CreateValidCommand(
            metaTitle: "Test Brand | Shop",
            metaDescription: "Shop the best products from Test Brand");

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        _brandRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand brand, CancellationToken _) => brand);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MetaTitle.Should().Be("Test Brand | Shop");
        result.Value.MetaDescription.Should().Be("Shop the best products from Test Brand");
    }

    [Fact]
    public async Task Handle_WithIsFeaturedTrue_ShouldSetFeatured()
    {
        // Arrange
        var command = CreateValidCommand(isFeatured: true);

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        _brandRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand brand, CancellationToken _) => brand);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsFeatured.Should().BeTrue();
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var command = CreateValidCommand(slug: "existing-slug");

        var existingBrand = Brand.Create("Existing Brand", "existing-slug", "tenant-123");

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Brand.DuplicateSlug);

        _brandRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        const string tenantId = "tenant-abc";
        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);

        var command = CreateValidCommand();

        Brand? capturedBrand = null;

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        _brandRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()))
            .Callback<Brand, CancellationToken>((brand, _) => capturedBrand = brand)
            .ReturnsAsync((Brand brand, CancellationToken _) => brand);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedBrand.Should().NotBeNull();
        capturedBrand!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var command = CreateValidCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandSlugExistsSpec>(),
                token))
            .ReturnsAsync((Brand?)null);

        _brandRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Brand>(), token))
            .ReturnsAsync((Brand brand, CancellationToken _) => brand);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _brandRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<BrandSlugExistsSpec>(), token),
            Times.Once);

        _brandRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Brand>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
