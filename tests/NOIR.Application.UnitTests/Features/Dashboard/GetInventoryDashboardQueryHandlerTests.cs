using NOIR.Application.Features.Dashboard;
using NOIR.Application.Features.Dashboard.DTOs;
using NOIR.Application.Features.Dashboard.Queries.GetInventoryDashboard;

namespace NOIR.Application.UnitTests.Features.Dashboard;

/// <summary>
/// Unit tests for GetInventoryDashboardQueryHandler.
/// Tests inventory dashboard aggregation with mocked IInventoryDashboardQueryService.
/// </summary>
public class GetInventoryDashboardQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IInventoryDashboardQueryService> _inventoryDashboardServiceMock;
    private readonly GetInventoryDashboardQueryHandler _handler;

    public GetInventoryDashboardQueryHandlerTests()
    {
        _inventoryDashboardServiceMock = new Mock<IInventoryDashboardQueryService>();
        _handler = new GetInventoryDashboardQueryHandler(_inventoryDashboardServiceMock.Object);
    }

    private static InventoryDashboardDto CreateTestInventoryDashboard()
    {
        var lowStockAlerts = new List<LowStockAlertDto>
        {
            new(Guid.NewGuid(), "Widget A", "SKU-001", 3, 10),
            new(Guid.NewGuid(), "Widget B", "SKU-002", 5, 10),
        }.AsReadOnly();

        var recentReceipts = new List<RecentReceiptDto>
        {
            new(Guid.NewGuid(), "RCV-001", "StockIn", DateTimeOffset.UtcNow.AddHours(-2), 15),
            new(Guid.NewGuid(), "SHP-001", "StockOut", DateTimeOffset.UtcNow.AddHours(-5), 8),
        }.AsReadOnly();

        var valueSummary = new InventoryValueSummaryDto(
            TotalValue: 500_000m,
            TotalSku: 200,
            InStockSku: 180,
            OutOfStockSku: 20);

        var stockMovementTrend = Enumerable.Range(0, 7)
            .Select(i => new StockMovementTrendDto(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                10 + i,
                5 + i))
            .ToList()
            .AsReadOnly();

        return new InventoryDashboardDto(
            lowStockAlerts,
            recentReceipts,
            valueSummary,
            stockMovementTrend);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultParameters_ShouldReturnDashboardSuccessfully()
    {
        // Arrange
        var query = new GetInventoryDashboardQuery();
        var expected = CreateTestInventoryDashboard();

        _inventoryDashboardServiceMock
            .Setup(x => x.GetInventoryDashboardAsync(10, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_ShouldReturnLowStockAlerts()
    {
        // Arrange
        var query = new GetInventoryDashboardQuery();
        var expected = CreateTestInventoryDashboard();

        _inventoryDashboardServiceMock
            .Setup(x => x.GetInventoryDashboardAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.LowStockAlerts.Count().ShouldBe(2);
        foreach (var a in result.Value.LowStockAlerts)
        {
            a.CurrentStock.ShouldBeLessThan(a.Threshold);
        }
    }

    [Fact]
    public async Task Handle_ShouldReturnValueSummary()
    {
        // Arrange
        var query = new GetInventoryDashboardQuery();
        var expected = CreateTestInventoryDashboard();

        _inventoryDashboardServiceMock
            .Setup(x => x.GetInventoryDashboardAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ValueSummary.TotalValue.ShouldBe(500_000m);
        result.Value.ValueSummary.TotalSku.ShouldBe(200);
        result.Value.ValueSummary.InStockSku.ShouldBe(180);
        result.Value.ValueSummary.OutOfStockSku.ShouldBe(20);
    }

    #endregion

    #region Parameter Forwarding

    [Fact]
    public async Task Handle_WithCustomParameters_ShouldPassToService()
    {
        // Arrange
        var query = new GetInventoryDashboardQuery(LowStockThreshold: 25, RecentReceiptsCount: 10);
        var expected = CreateTestInventoryDashboard();

        _inventoryDashboardServiceMock
            .Setup(x => x.GetInventoryDashboardAsync(25, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _inventoryDashboardServiceMock.Verify(
            x => x.GetInventoryDashboardAsync(25, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultParameterValues()
    {
        // Arrange
        var query = new GetInventoryDashboardQuery();
        var expected = CreateTestInventoryDashboard();

        _inventoryDashboardServiceMock
            .Setup(x => x.GetInventoryDashboardAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _inventoryDashboardServiceMock.Verify(
            x => x.GetInventoryDashboardAsync(10, 5, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ServiceCallCount_ShouldBeExactlyOnce()
    {
        // Arrange
        var query = new GetInventoryDashboardQuery();
        var expected = CreateTestInventoryDashboard();

        _inventoryDashboardServiceMock
            .Setup(x => x.GetInventoryDashboardAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _inventoryDashboardServiceMock.Verify(
            x => x.GetInventoryDashboardAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
