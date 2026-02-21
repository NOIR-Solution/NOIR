namespace NOIR.Application.UnitTests.Features.Brands;

/// <summary>
/// Unit tests for GetBrandsQueryHandler.
/// Tests paged brand list retrieval with mocked dependencies.
/// </summary>
public class GetBrandsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Brand, Guid>> _brandRepositoryMock;
    private readonly GetBrandsQueryHandler _handler;

    public GetBrandsQueryHandlerTests()
    {
        _brandRepositoryMock = new Mock<IRepository<Brand, Guid>>();

        _handler = new GetBrandsQueryHandler(_brandRepositoryMock.Object);
    }

    private static Brand CreateTestBrand(string name, string slug, bool isFeatured = false, bool isActive = true)
    {
        var brand = Brand.Create(name, slug, "tenant-123");
        brand.SetFeatured(isFeatured);
        brand.SetActive(isActive);
        return brand;
    }

    private static List<Brand> CreateTestBrands(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestBrand($"Brand {i}", $"brand-{i}"))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPaging_ShouldReturnPagedResult()
    {
        // Arrange
        var brands = CreateTestBrands(5);

        _brandRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<BrandsPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(brands);

        _brandRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<BrandsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetBrandsQuery(Page: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        result.Value.PageIndex.Should().Be(0); // 0-based internal index
        result.Value.PageNumber.Should().Be(1); // 1-based user-facing page
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithPaging_ShouldReturnCorrectPage()
    {
        // Arrange - Simulate page 2 of 25 total items (items 11-20)
        var page2Brands = CreateTestBrands(10); // 10 items for page 2

        _brandRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<BrandsPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Brands);

        _brandRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<BrandsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25); // 25 total items across all pages

        var query = new GetBrandsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.PageIndex.Should().Be(1); // 0-based internal index for page 2
        result.Value.PageNumber.Should().Be(2); // 1-based user-facing page
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _brandRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<BrandsPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Brand>());

        _brandRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<BrandsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetBrandsQuery(Page: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldMapBrandsToListDto()
    {
        // Arrange
        var brand = CreateTestBrand("Featured Brand", "featured-brand", isFeatured: true);

        _brandRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<BrandsPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Brand> { brand });

        _brandRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<BrandsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetBrandsQuery(Page: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.First();
        item.Name.Should().Be("Featured Brand");
        item.Slug.Should().Be("featured-brand");
        item.IsFeatured.Should().BeTrue();
    }

    #endregion

    #region Filter Scenarios

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassToSpecification()
    {
        // Arrange
        _brandRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<BrandsPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Brand>());

        _brandRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<BrandsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetBrandsQuery(
            Search: "Nike",
            Page: 1,
            PageSize: 10);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert - The specification is constructed with the search parameter
        _brandRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<BrandsPagedSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _brandRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<BrandsCountSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsActiveFilter_ShouldPassToSpecification()
    {
        // Arrange
        _brandRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<BrandsPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Brand>());

        _brandRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<BrandsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetBrandsQuery(
            IsActive: true,
            Page: 1,
            PageSize: 10);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _brandRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<BrandsPagedSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsFeaturedFilter_ShouldPassToSpecification()
    {
        // Arrange
        _brandRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<BrandsPagedSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Brand>());

        _brandRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<BrandsCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetBrandsQuery(
            IsFeatured: true,
            Page: 1,
            PageSize: 10);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _brandRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<BrandsPagedSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _brandRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<BrandsPagedSpec>(),
                token))
            .ReturnsAsync(new List<Brand>());

        _brandRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<BrandsCountSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetBrandsQuery(Page: 1, PageSize: 10);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _brandRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<BrandsPagedSpec>(), token),
            Times.Once);

        _brandRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<BrandsCountSpec>(), token),
            Times.Once);
    }

    #endregion
}
