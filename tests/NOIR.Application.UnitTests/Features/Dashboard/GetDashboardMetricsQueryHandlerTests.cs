using NOIR.Application.Features.Dashboard;
using NOIR.Application.Features.Dashboard.DTOs;
using NOIR.Application.Features.Dashboard.Queries.GetDashboardMetrics;

namespace NOIR.Application.UnitTests.Features.Dashboard;

/// <summary>
/// Unit tests for GetDashboardMetricsQueryHandler.
/// Tests dashboard metrics aggregation with mocked IDashboardQueryService.
/// </summary>
public class GetDashboardMetricsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IDashboardQueryService> _dashboardServiceMock;
    private readonly GetDashboardMetricsQueryHandler _handler;

    public GetDashboardMetricsQueryHandlerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardQueryService>();
        _handler = new GetDashboardMetricsQueryHandler(_dashboardServiceMock.Object);
    }

    private static DashboardMetricsDto CreateTestMetrics(
        decimal totalRevenue = 150_000m,
        int totalOrders = 42,
        int topProductsCount = 5,
        int lowStockCount = 3,
        int recentOrdersCount = 5)
    {
        var revenue = new RevenueMetricsDto(
            TotalRevenue: totalRevenue,
            RevenueThisMonth: 25_000m,
            RevenueLastMonth: 20_000m,
            RevenueToday: 1_500m,
            TotalOrders: totalOrders,
            OrdersThisMonth: 8,
            OrdersToday: 2,
            AverageOrderValue: totalOrders > 0 ? totalRevenue / totalOrders : 0m);

        var orderCounts = new OrderStatusCountsDto(
            Pending: 5,
            Confirmed: 3,
            Processing: 2,
            Shipped: 4,
            Delivered: 10,
            Completed: 15,
            Cancelled: 2,
            Refunded: 1,
            Returned: 0);

        var topProducts = Enumerable.Range(1, topProductsCount)
            .Select(i => new TopSellingProductDto(
                ProductId: Guid.NewGuid(),
                ProductName: $"Product {i}",
                ImageUrl: $"https://example.com/images/product-{i}.jpg",
                TotalQuantitySold: 100 - (i * 10),
                TotalRevenue: 5000m - (i * 500m)))
            .ToList()
            .AsReadOnly();

        var lowStockProducts = Enumerable.Range(1, lowStockCount)
            .Select(i => new LowStockProductDto(
                ProductId: Guid.NewGuid(),
                VariantId: Guid.NewGuid(),
                ProductName: $"Low Stock Product {i}",
                VariantName: $"Variant {i}",
                Sku: $"SKU-LOW-{i:D3}",
                StockQuantity: i,
                LowStockThreshold: 10))
            .ToList()
            .AsReadOnly();

        var recentOrders = Enumerable.Range(1, recentOrdersCount)
            .Select(i => new RecentOrderDto(
                OrderId: Guid.NewGuid(),
                OrderNumber: $"ORD-20260218-{i:D4}",
                CustomerEmail: $"customer{i}@example.com",
                GrandTotal: 100m * i,
                Status: OrderStatus.Pending,
                CreatedAt: DateTimeOffset.UtcNow.AddHours(-i)))
            .ToList()
            .AsReadOnly();

        var salesOverTime = Enumerable.Range(0, 7)
            .Select(i => new SalesOverTimeDto(
                Date: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                Revenue: 3000m + (i * 500m),
                OrderCount: 5 + i))
            .ToList()
            .AsReadOnly();

        var productDistribution = new ProductStatusDistributionDto(
            Draft: 5,
            Active: 30,
            Archived: 8);

        return new DashboardMetricsDto(
            Revenue: revenue,
            OrderCounts: orderCounts,
            TopSellingProducts: topProducts,
            LowStockProducts: lowStockProducts,
            RecentOrders: recentOrders,
            SalesOverTime: salesOverTime,
            ProductDistribution: productDistribution);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultParameters_ShouldReturnMetricsSuccessfully()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery();
        var expectedMetrics = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                query.TopProductsCount,
                query.LowStockThreshold,
                query.RecentOrdersCount,
                query.SalesOverTimeDays,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(expectedMetrics);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectRevenueMetrics()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery();
        var expectedMetrics = CreateTestMetrics(totalRevenue: 250_000m, totalOrders: 100);

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Revenue.TotalRevenue.ShouldBe(250_000m);
        result.Value.Revenue.TotalOrders.ShouldBe(100);
        result.Value.Revenue.RevenueThisMonth.ShouldBe(25_000m);
        result.Value.Revenue.RevenueLastMonth.ShouldBe(20_000m);
        result.Value.Revenue.AverageOrderValue.ShouldBe(2_500m);
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectOrderStatusCounts()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery();
        var expectedMetrics = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var counts = result.Value.OrderCounts;
        counts.Pending.ShouldBe(5);
        counts.Confirmed.ShouldBe(3);
        counts.Processing.ShouldBe(2);
        counts.Shipped.ShouldBe(4);
        counts.Delivered.ShouldBe(10);
        counts.Completed.ShouldBe(15);
        counts.Cancelled.ShouldBe(2);
        counts.Refunded.ShouldBe(1);
        counts.Returned.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_ShouldReturnTopSellingProducts()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery(TopProductsCount: 3);
        var expectedMetrics = CreateTestMetrics(topProductsCount: 3);

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                3, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.TopSellingProducts.Count().ShouldBe(3);
        foreach (var p in result.Value.TopSellingProducts)
        {
            p.ProductId.ShouldNotBe(Guid.Empty);
            p.ProductName.ShouldNotBeNullOrEmpty();
            p.TotalQuantitySold.ShouldBeGreaterThan(0);
            p.TotalRevenue.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Handle_ShouldReturnLowStockProducts()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery(LowStockThreshold: 15);
        var expectedMetrics = CreateTestMetrics(lowStockCount: 4);

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), 15, It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.LowStockProducts.Count().ShouldBe(4);
        foreach (var p in result.Value.LowStockProducts)
        {
            p.ProductId.ShouldNotBe(Guid.Empty);
            p.VariantId.ShouldNotBe(Guid.Empty);
            p.StockQuantity.ShouldBeLessThanOrEqualTo(10);
        }
    }

    [Fact]
    public async Task Handle_ShouldReturnRecentOrders()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery(RecentOrdersCount: 10);
        var expectedMetrics = CreateTestMetrics(recentOrdersCount: 5);

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), 10, It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.RecentOrders.Count().ShouldBe(5);
        foreach (var o in result.Value.RecentOrders)
        {
            o.OrderId.ShouldNotBe(Guid.Empty);
            o.OrderNumber.ShouldStartWith("ORD-");
            o.GrandTotal.ShouldBeGreaterThan(0);
        }
    }

    [Fact]
    public async Task Handle_ShouldReturnProductDistribution()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery();
        var expectedMetrics = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProductDistribution.Draft.ShouldBe(5);
        result.Value.ProductDistribution.Active.ShouldBe(30);
        result.Value.ProductDistribution.Archived.ShouldBe(8);
    }

    [Fact]
    public async Task Handle_ShouldReturnSalesOverTimeData()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery(SalesOverTimeDays: 7);
        var expectedMetrics = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), 7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.SalesOverTime.Count().ShouldBe(7);
        foreach (var s in result.Value.SalesOverTime)
        {
            s.Revenue.ShouldBeGreaterThan(0);
            s.OrderCount.ShouldBeGreaterThan(0);
        }
    }

    #endregion

    #region Parameter Forwarding

    [Fact]
    public async Task Handle_ShouldPassCustomParametersToService()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery(
            TopProductsCount: 10,
            LowStockThreshold: 20,
            RecentOrdersCount: 15,
            SalesOverTimeDays: 60);
        var expectedMetrics = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                10, 20, 15, 60,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

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
        var query = new GetDashboardMetricsQuery();
        var expectedMetrics = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _dashboardServiceMock.Verify(
            x => x.GetMetricsAsync(5, 10, 10, 30, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToService()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery();
        var expectedMetrics = CreateTestMetrics();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _dashboardServiceMock.Verify(
            x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                token),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithEmptyMetrics_ShouldReturnSuccessfully()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery();
        var emptyMetrics = new DashboardMetricsDto(
            Revenue: new RevenueMetricsDto(0, 0, 0, 0, 0, 0, 0, 0),
            OrderCounts: new OrderStatusCountsDto(0, 0, 0, 0, 0, 0, 0, 0, 0),
            TopSellingProducts: new List<TopSellingProductDto>().AsReadOnly(),
            LowStockProducts: new List<LowStockProductDto>().AsReadOnly(),
            RecentOrders: new List<RecentOrderDto>().AsReadOnly(),
            SalesOverTime: new List<SalesOverTimeDto>().AsReadOnly(),
            ProductDistribution: new ProductStatusDistributionDto(0, 0, 0));

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyMetrics);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Revenue.TotalRevenue.ShouldBe(0);
        result.Value.Revenue.TotalOrders.ShouldBe(0);
        result.Value.TopSellingProducts.ShouldBeEmpty();
        result.Value.LowStockProducts.ShouldBeEmpty();
        result.Value.RecentOrders.ShouldBeEmpty();
        result.Value.SalesOverTime.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_ServiceCallCount_ShouldBeExactlyOnce()
    {
        // Arrange
        var query = new GetDashboardMetricsQuery();
        var expectedMetrics = CreateTestMetrics();

        _dashboardServiceMock
            .Setup(x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMetrics);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _dashboardServiceMock.Verify(
            x => x.GetMetricsAsync(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
