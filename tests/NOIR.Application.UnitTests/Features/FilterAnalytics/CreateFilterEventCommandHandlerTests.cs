using NOIR.Application.Features.FilterAnalytics.Commands.CreateFilterEvent;
using NOIR.Application.Features.FilterAnalytics.DTOs;
using NOIR.Domain.Entities.Analytics;

namespace NOIR.Application.UnitTests.Features.FilterAnalytics;

/// <summary>
/// Unit tests for CreateFilterEventCommandHandler.
/// Tests creating filter analytics events for authenticated and guest users.
/// </summary>
public class CreateFilterEventCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<CreateFilterEventCommandHandler>> _loggerMock;
    private readonly Mock<DbSet<FilterAnalyticsEvent>> _mockDbSet;
    private readonly CreateFilterEventCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";
    private const string TestSessionId = "session-abc-123";

    public CreateFilterEventCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<CreateFilterEventCommandHandler>>();
        _mockDbSet = new Mock<DbSet<FilterAnalyticsEvent>>();

        _dbContextMock.Setup(x => x.FilterAnalyticsEvents).Returns(_mockDbSet.Object);
        _dbContextMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(TestUserId);

        _handler = new CreateFilterEventCommandHandler(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_AuthenticatedUser_FilterApplied_ReturnsSuccessWithAllFields()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 42,
            CategorySlug: "electronics",
            FilterCode: "brand",
            FilterValue: "Apple");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.SessionId.ShouldBe(TestSessionId);
        result.Value.UserId.ShouldBe(TestUserId);
        result.Value.EventType.ShouldBe(FilterEventType.FilterApplied);
        result.Value.ProductCount.ShouldBe(42);
        result.Value.CategorySlug.ShouldBe("electronics");
        result.Value.FilterCode.ShouldBe("brand");
        result.Value.FilterValue.ShouldBe("Apple");
        result.Value.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_GuestUser_ReturnsSuccessWithNullUserId()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);

        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.UserId.ShouldBeNull();
        result.Value.SessionId.ShouldBe(TestSessionId);
    }

    [Fact]
    public async Task Handle_ProductClickedEvent_ReturnsSuccessWithClickedProductId()
    {
        // Arrange
        var clickedProductId = Guid.NewGuid();
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.ProductClicked,
            ProductCount: 0,
            ClickedProductId: clickedProductId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.EventType.ShouldBe(FilterEventType.ProductClicked);
        result.Value.ClickedProductId.ShouldBe(clickedProductId);
    }

    [Fact]
    public async Task Handle_SearchPerformedEvent_ReturnsSuccessWithSearchQuery()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.SearchPerformed,
            ProductCount: 25,
            SearchQuery: "wireless headphones");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.EventType.ShouldBe(FilterEventType.SearchPerformed);
        result.Value.SearchQuery.ShouldBe("wireless headphones");
        result.Value.ProductCount.ShouldBe(25);
    }

    [Fact]
    public async Task Handle_AllOptionalFieldsPopulated_ReturnsSuccessWithAllFields()
    {
        // Arrange
        var clickedProductId = Guid.NewGuid();
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 15,
            CategorySlug: "shoes",
            FilterCode: "color",
            FilterValue: "red",
            SearchQuery: "running shoes",
            ClickedProductId: clickedProductId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.CategorySlug.ShouldBe("shoes");
        result.Value.FilterCode.ShouldBe("color");
        result.Value.FilterValue.ShouldBe("red");
        result.Value.SearchQuery.ShouldBe("running shoes");
        result.Value.ClickedProductId.ShouldBe(clickedProductId);
    }

    [Fact]
    public async Task Handle_MinimalFields_ReturnsSuccessWithNullOptionals()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.SessionId.ShouldBe(TestSessionId);
        result.Value.EventType.ShouldBe(FilterEventType.FilterApplied);
        result.Value.ProductCount.ShouldBe(0);
        result.Value.CategorySlug.ShouldBeNull();
        result.Value.FilterCode.ShouldBeNull();
        result.Value.FilterValue.ShouldBeNull();
        result.Value.SearchQuery.ShouldBeNull();
        result.Value.ClickedProductId.ShouldBeNull();
    }

    #endregion

    #region Verification

    [Fact]
    public async Task Handle_ShouldCallDbSetAdd()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockDbSet.Verify(
            x => x.Add(It.Is<FilterAnalyticsEvent>(e =>
                e.SessionId == TestSessionId &&
                e.EventType == FilterEventType.FilterApplied)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCallSaveChangesAsync()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Tenant Context

    [Fact]
    public async Task Handle_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        const string customTenantId = "custom-tenant-999";
        _currentUserMock.Setup(x => x.TenantId).Returns(customTenantId);

        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 5);

        FilterAnalyticsEvent? capturedEvent = null;
        _mockDbSet
            .Setup(x => x.Add(It.IsAny<FilterAnalyticsEvent>()))
            .Callback<FilterAnalyticsEvent>(e => capturedEvent = e);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedEvent.ShouldNotBeNull();
        capturedEvent!.TenantId.ShouldBe(customTenantId);
    }

    [Fact]
    public async Task Handle_WhenTenantIdIsNull_ShouldStillSucceed()
    {
        // Arrange
        _currentUserMock.Setup(x => x.TenantId).Returns((string?)null);

        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
    }

    #endregion

    #region Event Type Coverage

    [Theory]
    [InlineData(FilterEventType.FilterApplied)]
    [InlineData(FilterEventType.FilterRemoved)]
    [InlineData(FilterEventType.FilterCleared)]
    [InlineData(FilterEventType.SearchPerformed)]
    [InlineData(FilterEventType.ResultsViewed)]
    [InlineData(FilterEventType.ProductClicked)]
    public async Task Handle_WithAllEventTypes_ShouldSucceed(FilterEventType eventType)
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: eventType,
            ProductCount: 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.EventType.ShouldBe(eventType);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldReturnDtoWithNonEmptyId()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtoWithCreatedAtTimestamp()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 0);

        var beforeTime = DateTimeOffset.UtcNow.AddSeconds(-1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.CreatedAt.ShouldBeGreaterThan(beforeTime);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToSaveChanges()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 0);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _dbContextMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_GuestUser_ShouldStillPersistEvent()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);

        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 10,
            FilterCode: "price",
            FilterValue: "100-500");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        _mockDbSet.Verify(
            x => x.Add(It.IsAny<FilterAnalyticsEvent>()),
            Times.Once);
        _dbContextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithZeroProductCount_ShouldSucceed()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 0);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProductCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WithLargeProductCount_ShouldSucceed()
    {
        // Arrange
        var command = new CreateFilterEventCommand(
            SessionId: TestSessionId,
            EventType: FilterEventType.FilterApplied,
            ProductCount: 999999);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProductCount.ShouldBe(999999);
    }

    #endregion
}
