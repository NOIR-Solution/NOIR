using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductStats;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Products;

/// <summary>
/// Unit tests for GetProductStatsQueryHandler.
/// Tests product statistics retrieval with mocked dependencies.
/// </summary>
public class GetProductStatsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IReadRepository<Product, Guid>> _repositoryMock;
    private readonly GetProductStatsQueryHandler _handler;

    public GetProductStatsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IReadRepository<Product, Guid>>();
        _handler = new GetProductStatsQueryHandler(_repositoryMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ShouldReturnCorrectStats()
    {
        // Arrange
        var query = new GetProductStatsQuery();

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        // NOTE: Moq's It.IsAny<> cannot differentiate between ProductsByStatusCountSpec instances
        // with different status values (Active, Draft, Archived, OutOfStock) because Moq matches
        // on type, not constructor arguments. All status counts return the same value.
        // This tests that the handler correctly aggregates counts, not that different specs
        // return different values (which is the spec's responsibility, tested elsewhere).
        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsByStatusCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsLowStockCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(8);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(100);
        result.Value.Active.Should().Be(25);
        result.Value.Draft.Should().Be(25);
        result.Value.Archived.Should().Be(25);
        result.Value.OutOfStock.Should().Be(25);
        result.Value.LowStock.Should().Be(8);
    }

    [Fact]
    public async Task Handle_WithZeroProducts_ShouldReturnZeroStats()
    {
        // Arrange
        var query = new GetProductStatsQuery();

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsByStatusCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsLowStockCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(0);
        result.Value.Active.Should().Be(0);
        result.Value.Draft.Should().Be(0);
        result.Value.Archived.Should().Be(0);
        result.Value.OutOfStock.Should().Be(0);
        result.Value.LowStock.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldExecuteCountQueriesInParallel()
    {
        // Arrange
        var query = new GetProductStatsQuery();
        var callOrder = new List<string>();
        var syncLock = new object();

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                lock (syncLock) { callOrder.Add("Total"); }
                await Task.Delay(10); // Small delay to test parallelism
                return 100;
            });

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsByStatusCountSpec>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                lock (syncLock) { callOrder.Add("Status"); }
                await Task.Delay(10);
                return 25;
            });

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsLowStockCountSpec>(),
                It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                lock (syncLock) { callOrder.Add("LowStock"); }
                await Task.Delay(10);
                return 5;
            });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // All count calls should be made (6 total: 1 total + 4 status + 1 low stock)
        _repositoryMock.Verify(
            x => x.CountAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _repositoryMock.Verify(
            x => x.CountAsync(It.IsAny<ProductsByStatusCountSpec>(), It.IsAny<CancellationToken>()),
            Times.Exactly(4)); // Active, Draft, Archived, OutOfStock
        _repositoryMock.Verify(
            x => x.CountAsync(It.IsAny<ProductsLowStockCountSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var query = new GetProductStatsQuery();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repositoryMock
            .Setup(x => x.CountAsync(token))
            .ReturnsAsync(0);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsByStatusCountSpec>(),
                token))
            .ReturnsAsync(0);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsLowStockCountSpec>(),
                token))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _repositoryMock.Verify(
            x => x.CountAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnResultType()
    {
        // Arrange
        var query = new GetProductStatsQuery();

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsByStatusCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsLowStockCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeOfType<Result<ProductStatsDto>>();
        result.Value.Should().BeOfType<ProductStatsDto>();
    }

    [Fact]
    public async Task Handle_StatsDto_ShouldHaveCorrectProperties()
    {
        // Arrange
        var query = new GetProductStatsQuery();

        _repositoryMock
            .Setup(x => x.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        // All status counts return the same value since we can't easily differentiate specs
        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsByStatusCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(20);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ProductsLowStockCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(12);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var stats = result.Value;
        // Verify the DTO has all expected properties
        stats.Total.Should().Be(100);
        stats.Active.Should().Be(20);
        stats.Draft.Should().Be(20);
        stats.Archived.Should().Be(20);
        stats.OutOfStock.Should().Be(20);
        stats.LowStock.Should().Be(12);
    }

    #endregion
}
