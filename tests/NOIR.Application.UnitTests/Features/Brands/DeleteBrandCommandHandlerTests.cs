namespace NOIR.Application.UnitTests.Features.Brands;

/// <summary>
/// Unit tests for DeleteBrandCommandHandler.
/// Tests brand deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteBrandCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Brand, Guid>> _brandRepositoryMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteBrandCommandHandler _handler;

    public DeleteBrandCommandHandlerTests()
    {
        _brandRepositoryMock = new Mock<IRepository<Brand, Guid>>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteBrandCommandHandler(
            _brandRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Brand CreateTestBrand(string name = "Test Brand", string slug = "test-brand")
    {
        return Brand.Create(name, slug, "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenBrandExistsAndHasNoProducts_ShouldSucceed()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = CreateTestBrand();

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        _productRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<BrandHasProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteBrandCommand(brandId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        _brandRepositoryMock.Verify(
            x => x.Remove(existingBrand),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallMarkAsDeletedBeforeRemove()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = CreateTestBrand();
        var initialEventCount = existingBrand.DomainEvents.Count;

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        _productRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<BrandHasProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new DeleteBrandCommand(brandId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - MarkAsDeleted should add a BrandDeletedEvent
        existingBrand.DomainEvents.Count.Should().BeGreaterThan(initialEventCount);
        existingBrand.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "BrandDeletedEvent");
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

        var command = new DeleteBrandCommand(brandId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Brand.NotFound);

        _brandRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Brand>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenBrandHasProducts_ShouldReturnConflict()
    {
        // Arrange
        var brandId = Guid.NewGuid();
        var existingBrand = CreateTestBrand();

        _brandRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<BrandByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBrand);

        _productRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<BrandHasProductsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new DeleteBrandCommand(brandId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Brand.HasProducts);

        _brandRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Brand>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepositories()
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

        _productRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<BrandHasProductsSpec>(),
                token))
            .ReturnsAsync(false);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new DeleteBrandCommand(brandId);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _brandRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<BrandByIdForUpdateSpec>(), token),
            Times.Once);

        _productRepositoryMock.Verify(
            x => x.AnyAsync(It.IsAny<BrandHasProductsSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
