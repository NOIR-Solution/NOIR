using NOIR.Application.Features.Dashboard;
using NOIR.Application.Features.Dashboard.DTOs;
using NOIR.Application.Features.Dashboard.Queries.GetEcommerceDashboard;

namespace NOIR.Application.UnitTests.Features.Dashboard;

/// <summary>
/// Unit tests for GetEcommerceDashboardQueryHandler.
/// Tests ecommerce dashboard aggregation with mocked IDashboardQueryService.
/// </summary>
public class GetEcommerceDashboardQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IDashboardQueryService> _dashboardServiceMock;
    private readonly GetEcommerceDashboardQueryHandler _handler;

    public GetEcommerceDashboardQueryHandlerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardQueryService>();
        _handler = new GetEcommerceDashboardQueryHandler(_dashboardServiceMock.Object);
    }

    private static DashboardMetricsDto CreateTestMetrics()
    {
        var revenue = new RevenueMetricsDto(
            TotalRevenue: 100_000m,
            RevenueThisMonth: 15_000m,
            RevenueLastMonth: 12_000m,
            RevenueToday: 800m,
            TotalOrders: 50,
            OrdersThisMonth: 10,
            OrdersToday: 2,
            AverageOrderValue: 2_000m);

        var orderCounts = new OrderStatusCountsDto(
            Pending: 3, Confirmed: 2, Processing: 1,
            Shipped: 5, Delivered: 15, Completed: 20,
            Cancelled: 2, Refunded: 1, Returned: 1);

        return new DashboardMetricsDto(
            Revenue: revenue,
            OrderCounts: orderCounts,
            TopSellingProducts: new List<TopSellingProductDto>().AsReadOnly(),
            LowStockProducts: new List<LowStockProductDto>().AsReadOnly(),
            RecentOrders: new List<RecentOrderDto>().AsReadOnly(),
            SalesOverTime: new List<SalesOverTimeDto>().AsReadOnly(),
            ProductDistribution: new ProductStatusDistributionDto(5, 30, 8));
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultParameters_ShouldReturnMetricsSuccessfully()
    {
        // Arrange
        var query = new GetEcommerceDashboardQuery();
        var expected = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                query.TopProductsCount, query.LowStockThreshold,
                query.RecentOrdersCount, query.SalesOverTimeDays,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectRevenueData()
    {
        // Arrange
        var query = new GetEcommerceDashboardQuery();
        var expected = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Revenue.TotalRevenue.ShouldBe(100_000m);
        result.Value.Revenue.TotalOrders.ShouldBe(50);
    }

    #endregion

    #region Parameter Forwarding

    [Fact]
    public async Task Handle_WithCustomParameters_ShouldPassToService()
    {
        // Arrange
        var query = new GetEcommerceDashboardQuery(
            TopProductsCount: 10,
            LowStockThreshold: 20,
            RecentOrdersCount: 15,
            SalesOverTimeDays: 60);
        var expected = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                10, 20, 15, 60,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _dashboardServiceMock.Verify(
            x => x.GetMetricsAsync(10, 20, 15, 60, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultParameterValues()
    {
        // Arrange
        var query = new GetEcommerceDashboardQuery();
        var expected = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _dashboardServiceMock.Verify(
            x => x.GetMetricsAsync(5, 10, 10, 30, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ServiceCallCount_ShouldBeExactlyOnce()
    {
        // Arrange
        var query = new GetEcommerceDashboardQuery();
        var expected = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _dashboardServiceMock.Verify(
            x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
