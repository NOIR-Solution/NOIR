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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.SessionId.Should().Be(TestSessionId);
        result.Value.UserId.Should().Be(TestUserId);
        result.Value.EventType.Should().Be(FilterEventType.FilterApplied);
        result.Value.ProductCount.Should().Be(42);
        result.Value.CategorySlug.Should().Be("electronics");
        result.Value.FilterCode.Should().Be("brand");
        result.Value.FilterValue.Should().Be("Apple");
        result.Value.Id.Should().NotBe(Guid.Empty);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().BeNull();
        result.Value.SessionId.Should().Be(TestSessionId);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be(FilterEventType.ProductClicked);
        result.Value.ClickedProductId.Should().Be(clickedProductId);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be(FilterEventType.SearchPerformed);
        result.Value.SearchQuery.Should().Be("wireless headphones");
        result.Value.ProductCount.Should().Be(25);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.CategorySlug.Should().Be("shoes");
        result.Value.FilterCode.Should().Be("color");
        result.Value.FilterValue.Should().Be("red");
        result.Value.SearchQuery.Should().Be("running shoes");
        result.Value.ClickedProductId.Should().Be(clickedProductId);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().Be(TestSessionId);
        result.Value.EventType.Should().Be(FilterEventType.FilterApplied);
        result.Value.ProductCount.Should().Be(0);
        result.Value.CategorySlug.Should().BeNull();
        result.Value.FilterCode.Should().BeNull();
        result.Value.FilterValue.Should().BeNull();
        result.Value.SearchQuery.Should().BeNull();
        result.Value.ClickedProductId.Should().BeNull();
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
        capturedEvent.Should().NotBeNull();
        capturedEvent!.TenantId.Should().Be(customTenantId);
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
        result.IsSuccess.Should().BeTrue();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be(eventType);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBe(Guid.Empty);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().BeAfter(beforeTime);
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
        result.IsSuccess.Should().BeTrue();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductCount.Should().Be(0);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.ProductCount.Should().Be(999999);
    }

    #endregion
}
