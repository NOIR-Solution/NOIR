namespace NOIR.Application.UnitTests.Features.Brands;

/// <summary>
/// Unit tests for GetBrandByIdQueryHandler.
/// Tests brand retrieval by ID with mocked dependencies.
/// </summary>
public class GetBrandByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Brand, Guid>> _brandRepositoryMock;
    private readonly GetBrandByIdQueryHandler _handler;

    public GetBrandByIdQueryHandlerTests()
    {
        _brandRepositoryMock = new Mock<IRepository<Brand, Guid>>();

        _handler = new GetBrandByIdQueryHandler(_brandRepositoryMock.Object);
    }

    private static Brand CreateTestBrand(
        string name = "Test Brand",
        string slug = "test-brand")
    {
        var brand = Brand.Create(name, slug, "tenant-123");
        brand.UpdateDetails(name, slug, "A test brand", "https://test.com");
        brand.UpdateBranding("https://cdn.test.com/logo.png", "https://cdn.test.com/banner.png");
        brand.UpdateSeo("Test Brand | Shop", "Shop Test Brand products");
        brand.SetFeatured(true);
        return brand;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenBrandExists_ShouldReturnBrandDto()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = CreateTestBrand();

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        var query = new GetBrandByIdQuery(brandId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Test Brand");
        result.Value.Slug.ShouldBe("test-brand");
        result.Value.Description.ShouldBe("A test brand");
        result.Value.Website.ShouldBe("https://test.com");
        result.Value.LogoUrl.ShouldBe("https://cdn.test.com/logo.png");
        result.Value.BannerUrl.ShouldBe("https://cdn.test.com/banner.png");
        result.Value.MetaTitle.ShouldBe("Test Brand | Shop");
        result.Value.MetaDescription.ShouldBe("Shop Test Brand products");
        result.Value.IsFeatured.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithMinimalBrand_ShouldReturnBrandDto()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = Brand.Create("Minimal Brand", "minimal-brand", "tenant-123");

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        var query = new GetBrandByIdQuery(brandId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Name.ShouldBe("Minimal Brand");
        result.Value.Slug.ShouldBe("minimal-brand");
        result.Value.Description.ShouldBeNull();
        result.Value.Website.ShouldBeNull();
        result.Value.LogoUrl.ShouldBeNull();
        result.Value.BannerUrl.ShouldBeNull();
        result.Value.IsFeatured.ShouldBe(false);
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
                It.IsAny<BrandByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Brand?)null);

        var query = new GetBrandByIdQuery(brandId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe(ErrorCodes.Brand.NotFound);
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
                It.IsAny<BrandByIdSpec>(),
                token))
            .ReturnsAsync(existingBrand);

        var query = new GetBrandByIdQuery(brandId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _brandRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<BrandByIdSpec>(), token),
            Times.Once);
    }

    #endregion
}
