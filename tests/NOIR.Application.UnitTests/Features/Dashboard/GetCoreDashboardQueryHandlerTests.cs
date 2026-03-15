using NOIR.Application.Features.Dashboard;
using NOIR.Application.Features.Dashboard.DTOs;
using NOIR.Application.Features.Dashboard.Queries.GetCoreDashboard;

namespace NOIR.Application.UnitTests.Features.Dashboard;

/// <summary>
/// Unit tests for GetCoreDashboardQueryHandler.
/// Tests core dashboard aggregation with mocked ICoreDashboardQueryService.
/// </summary>
public class GetCoreDashboardQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<ICoreDashboardQueryService> _coreDashboardServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GetCoreDashboardQueryHandler _handler;

    public GetCoreDashboardQueryHandlerTests()
    {
        _coreDashboardServiceMock = new Mock<ICoreDashboardQueryService>();
        _currentUserMock = new Mock<ICurrentUser>();

        _handler = new GetCoreDashboardQueryHandler(
            _coreDashboardServiceMock.Object,
            _currentUserMock.Object);
    }

    private static CoreDashboardDto CreateTestCoreDashboard(bool includeSystemHealth = false)
    {
        var quickActions = new QuickActionCountsDto(
            PendingOrders: 5,
            PendingReviews: 3,
            LowStockAlerts: 2,
            DraftProducts: 8);

        var recentActivity = new List<ActivityFeedItemDto>
        {
            new("order", "New Order", "Order #ORD-001 placed", DateTimeOffset.UtcNow.AddMinutes(-10), Guid.NewGuid().ToString(), "/orders/1", "customer@test.com", "Order #ORD-001"),
            new("product", "Product Updated", "Product 'Widget' updated", DateTimeOffset.UtcNow.AddMinutes(-30), Guid.NewGuid().ToString(), "/products/1", "admin@test.com", "Widget"),
        }.AsReadOnly();

        SystemHealthDto? systemHealth = includeSystemHealth
            ? new SystemHealthDto(
                ApiHealthy: true,
                BackgroundJobsQueued: 5,
                BackgroundJobsFailed: 0,
                ActiveTenants: 3)
            : null;

        return new CoreDashboardDto(quickActions, recentActivity, systemHealth);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultParameters_ShouldReturnDashboardSuccessfully()
    {
        // Arrange
        var query = new GetCoreDashboardQuery();
        var expected = CreateTestCoreDashboard();

        _currentUserMock.Setup(x => x.IsPlatformAdmin).Returns(false);

        _coreDashboardServiceMock
            .Setup(x => x.GetCoreDashboardAsync(
                10, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(expected);
        result.Value.QuickActions.PendingOrders.ShouldBe(5);
        result.Value.RecentActivity.Count().ShouldBe(2);
    }

    [Fact]
    public async Task Handle_WhenPlatformAdmin_ShouldIncludeSystemHealth()
    {
        // Arrange
        var query = new GetCoreDashboardQuery();
        var expected = CreateTestCoreDashboard(includeSystemHealth: true);

        _currentUserMock.Setup(x => x.IsPlatformAdmin).Returns(true);

        _coreDashboardServiceMock
            .Setup(x => x.GetCoreDashboardAsync(
                10, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.SystemHealth.ShouldNotBeNull();
        result.Value.SystemHealth!.ApiHealthy.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_WithCustomActivityCount_ShouldPassToService()
    {
        // Arrange
        var query = new GetCoreDashboardQuery(ActivityCount: 25);
        var expected = CreateTestCoreDashboard();

        _currentUserMock.Setup(x => x.IsPlatformAdmin).Returns(false);

        _coreDashboardServiceMock
            .Setup(x => x.GetCoreDashboardAsync(
                25, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _coreDashboardServiceMock.Verify(
            x => x.GetCoreDashboardAsync(25, false, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Parameter Forwarding

    [Fact]
    public async Task Handle_ShouldPassIsPlatformAdminToService()
    {
        // Arrange
        var query = new GetCoreDashboardQuery();
        var expected = CreateTestCoreDashboard();

        _currentUserMock.Setup(x => x.IsPlatformAdmin).Returns(true);

        _coreDashboardServiceMock
            .Setup(x => x.GetCoreDashboardAsync(
                It.IsAny<int>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _coreDashboardServiceMock.Verify(
            x => x.GetCoreDashboardAsync(It.IsAny<int>(), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToService()
    {
        // Arrange
        var query = new GetCoreDashboardQuery();
        var expected = CreateTestCoreDashboard();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _currentUserMock.Setup(x => x.IsPlatformAdmin).Returns(false);

        _coreDashboardServiceMock
            .Setup(x => x.GetCoreDashboardAsync(
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _coreDashboardServiceMock.Verify(
            x => x.GetCoreDashboardAsync(It.IsAny<int>(), It.IsAny<bool>(), token),
            Times.Once);
    }

    #endregion
}
