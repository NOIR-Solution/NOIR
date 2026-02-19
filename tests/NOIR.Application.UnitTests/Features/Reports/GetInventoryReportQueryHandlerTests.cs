using NOIR.Application.Features.Reports;
using NOIR.Application.Features.Reports.DTOs;
using NOIR.Application.Features.Reports.Queries.GetInventoryReport;

namespace NOIR.Application.UnitTests.Features.Reports;

/// <summary>
/// Unit tests for GetInventoryReportQueryHandler.
/// Tests delegation to IReportQueryService.GetInventoryReportAsync with different thresholds.
/// </summary>
public class GetInventoryReportQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IReportQueryService> _reportServiceMock;
    private readonly GetInventoryReportQueryHandler _handler;

    public GetInventoryReportQueryHandlerTests()
    {
        _reportServiceMock = new Mock<IReportQueryService>();
        _handler = new GetInventoryReportQueryHandler(_reportServiceMock.Object);
    }

    private static InventoryReportDto CreateInventoryReport(int lowStockCount = 3) =>
        new(
            LowStockProducts: Enumerable.Range(1, lowStockCount)
                .Select(i => new LowStockDto(
                    ProductId: Guid.NewGuid(),
                    Name: $"Low Stock Item {i}",
                    VariantSku: $"SKU-{i:D4}",
                    CurrentStock: i,
                    ReorderLevel: 10))
                .ToList(),
            TotalProducts: 500,
            TotalVariants: 1200,
            TotalStockValue: 250000m,
            TurnoverRate: 3.5m);

    #endregion

    #region Happy Path - Default Threshold

    [Fact]
    public async Task Handle_WithDefaultThreshold_ReturnsSuccess()
    {
        // Arrange
        var expectedReport = CreateInventoryReport();

        _reportServiceMock
            .Setup(x => x.GetInventoryReportAsync(
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetInventoryReportQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.LowStockProducts.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithDefaultThreshold_PassesTenToService()
    {
        // Arrange
        var expectedReport = CreateInventoryReport();

        _reportServiceMock
            .Setup(x => x.GetInventoryReportAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetInventoryReportQuery();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            x => x.GetInventoryReportAsync(10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Different Threshold Values

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task Handle_WithDifferentThresholds_PassesThresholdToService(int threshold)
    {
        // Arrange
        var expectedReport = CreateInventoryReport();

        _reportServiceMock
            .Setup(x => x.GetInventoryReportAsync(
                threshold,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetInventoryReportQuery(LowStockThreshold: threshold);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetInventoryReportAsync(threshold, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExplicitThreshold_PassesExactValueToService()
    {
        // Arrange
        var expectedReport = CreateInventoryReport(5);

        _reportServiceMock
            .Setup(x => x.GetInventoryReportAsync(
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetInventoryReportQuery(LowStockThreshold: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _reportServiceMock.Verify(
            x => x.GetInventoryReportAsync(20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Service Result Passthrough

    [Fact]
    public async Task Handle_ReturnsExactDtoFromService()
    {
        // Arrange
        var expectedReport = new InventoryReportDto(
            LowStockProducts: new List<LowStockDto>
            {
                new(Guid.NewGuid(), "Widget A", "SKU-W-001", 2, 15),
                new(Guid.NewGuid(), "Gadget B", null, 0, 10),
            },
            TotalProducts: 1000,
            TotalVariants: 3000,
            TotalStockValue: 500000m,
            TurnoverRate: 4.2m);

        _reportServiceMock
            .Setup(x => x.GetInventoryReportAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetInventoryReportQuery(LowStockThreshold: 15);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expectedReport);
        result.Value.TotalProducts.Should().Be(1000);
        result.Value.TotalVariants.Should().Be(3000);
        result.Value.TotalStockValue.Should().Be(500000m);
        result.Value.TurnoverRate.Should().Be(4.2m);
        result.Value.LowStockProducts.Should().HaveCount(2);
        result.Value.LowStockProducts[0].Name.Should().Be("Widget A");
        result.Value.LowStockProducts[1].VariantSku.Should().BeNull();
    }

    #endregion

    #region Cancellation Token

    [Fact]
    public async Task Handle_PassesCancellationTokenToService()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;
        var expectedReport = CreateInventoryReport();

        _reportServiceMock
            .Setup(x => x.GetInventoryReportAsync(
                It.IsAny<int>(),
                token))
            .ReturnsAsync(expectedReport);

        var query = new GetInventoryReportQuery(LowStockThreshold: 10);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _reportServiceMock.Verify(
            x => x.GetInventoryReportAsync(It.IsAny<int>(), token),
            Times.Once);
    }

    #endregion

    #region Empty Results

    [Fact]
    public async Task Handle_WithNoLowStock_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var expectedReport = new InventoryReportDto(
            LowStockProducts: new List<LowStockDto>(),
            TotalProducts: 500,
            TotalVariants: 1200,
            TotalStockValue: 250000m,
            TurnoverRate: 3.5m);

        _reportServiceMock
            .Setup(x => x.GetInventoryReportAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetInventoryReportQuery(LowStockThreshold: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.LowStockProducts.Should().BeEmpty();
        result.Value.TotalProducts.Should().Be(500);
    }

    #endregion

    #region Service Exception Propagation

    [Fact]
    public async Task Handle_WhenServiceThrows_ExceptionPropagates()
    {
        // Arrange
        _reportServiceMock
            .Setup(x => x.GetInventoryReportAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        var query = new GetInventoryReportQuery(LowStockThreshold: 10);

        // Act
        var act = () => _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Database connection failed");
    }

    #endregion

    #region Service Invocation Verification

    [Fact]
    public async Task Handle_CallsServiceExactlyOnce()
    {
        // Arrange
        var expectedReport = CreateInventoryReport();

        _reportServiceMock
            .Setup(x => x.GetInventoryReportAsync(
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedReport);

        var query = new GetInventoryReportQuery(LowStockThreshold: 30);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reportServiceMock.Verify(
            x => x.GetInventoryReportAsync(30, CancellationToken.None),
            Times.Once);

        _reportServiceMock.VerifyNoOtherCalls();
    }

    #endregion
}
