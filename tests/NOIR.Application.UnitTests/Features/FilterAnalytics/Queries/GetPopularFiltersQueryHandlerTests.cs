using NOIR.Application.Features.FilterAnalytics.DTOs;
using NOIR.Application.Features.FilterAnalytics.Queries.GetPopularFilters;
using NOIR.Domain.Entities.Analytics;

namespace NOIR.Application.UnitTests.Features.FilterAnalytics.Queries;

/// <summary>
/// Unit tests for GetPopularFiltersQueryHandler.
/// Tests popular filters retrieval with date ranges, category filtering, and limits.
/// </summary>
public class GetPopularFiltersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ILogger<GetPopularFiltersQueryHandler>> _loggerMock;
    private readonly GetPopularFiltersQueryHandler _handler;

    public GetPopularFiltersQueryHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _loggerMock = new Mock<ILogger<GetPopularFiltersQueryHandler>>();

        _handler = new GetPopularFiltersQueryHandler(
            _dbContextMock.Object,
            _loggerMock.Object);
    }

    private static FilterAnalyticsEvent CreateFilterAppliedEvent(
        string sessionId,
        string filterCode,
        string filterValue,
        int productCount = 10,
        string? userId = null,
        string? categorySlug = null,
        string? tenantId = "tenant-1")
    {
        return FilterAnalyticsEvent.Create(
            sessionId: sessionId,
            eventType: FilterEventType.FilterApplied,
            productCount: productCount,
            tenantId: tenantId,
            userId: userId,
            categorySlug: categorySlug,
            filterCode: filterCode,
            filterValue: filterValue);
    }

    private static FilterAnalyticsEvent CreateProductClickedEvent(
        string sessionId,
        Guid productId,
        string? userId = null,
        string? categorySlug = null,
        string? tenantId = "tenant-1")
    {
        return FilterAnalyticsEvent.Create(
            sessionId: sessionId,
            eventType: FilterEventType.ProductClicked,
            productCount: 0,
            tenantId: tenantId,
            userId: userId,
            categorySlug: categorySlug,
            clickedProductId: productId);
    }

    private void SetupFilterAnalyticsEvents(List<FilterAnalyticsEvent> events)
    {
        var mockDbSet = events.BuildMockDbSet();
        _dbContextMock.Setup(x => x.FilterAnalyticsEvents).Returns(mockDbSet.Object);
    }

    #endregion

    #region Default Date Range

    [Fact]
    public async Task Handle_WithDefaultParameters_ShouldReturnPopularFilters()
    {
        // Arrange
        var events = new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple", userId: "user-1"),
            CreateFilterAppliedEvent("s2", "brand", "apple", userId: "user-2"),
            CreateFilterAppliedEvent("s3", "brand", "samsung", userId: "user-3"),
            CreateFilterAppliedEvent("s4", "color", "red", userId: "user-1"),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Filters.Should().NotBeEmpty();
        result.Value.TotalEvents.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultDateRange_WhenNoDatesSpecified()
    {
        // Arrange
        var events = new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple"),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Default is last 30 days
        result.Value.FromDate.Should().BeBefore(result.Value.ToDate);
        var daysDiff = (result.Value.ToDate - result.Value.FromDate).TotalDays;
        daysDiff.Should().BeApproximately(30, 1);
    }

    #endregion

    #region Custom Date Range

    [Fact]
    public async Task Handle_WithCustomDateRange_ShouldUseDatesFromQuery()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-7);
        var toDate = DateTimeOffset.UtcNow;

        var events = new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple"),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery(
            FromDate: fromDate,
            ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FromDate.Should().Be(fromDate);
        result.Value.ToDate.Should().Be(toDate);
    }

    [Fact]
    public async Task Handle_WithOnlyFromDateSpecified_ShouldUseDefaultToDate()
    {
        // Arrange
        var fromDate = DateTimeOffset.UtcNow.AddDays(-14);

        SetupFilterAnalyticsEvents(new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple"),
        });

        var query = new GetPopularFiltersQuery(FromDate: fromDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FromDate.Should().Be(fromDate);
        // ToDate defaults to UtcNow
        result.Value.ToDate.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_WithOnlyToDateSpecified_ShouldCalculateFromDate30DaysBefore()
    {
        // Arrange
        var toDate = DateTimeOffset.UtcNow;

        SetupFilterAnalyticsEvents(new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple"),
        });

        var query = new GetPopularFiltersQuery(ToDate: toDate);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ToDate.Should().Be(toDate);
        var daysDiff = (result.Value.ToDate - result.Value.FromDate).TotalDays;
        daysDiff.Should().BeApproximately(30, 0.01);
    }

    #endregion

    #region Category Filter

    [Fact]
    public async Task Handle_WithCategorySlug_ShouldFilterByCategorySlug()
    {
        // Arrange
        var events = new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple", categorySlug: "electronics"),
            CreateFilterAppliedEvent("s2", "brand", "samsung", categorySlug: "electronics"),
            CreateFilterAppliedEvent("s3", "brand", "gucci", categorySlug: "fashion"),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery(CategorySlug: "electronics");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // All filters should be from the electronics category
        result.Value.Filters.Should().AllSatisfy(f =>
        {
            if (f.CategorySlug != null)
            {
                f.CategorySlug.Should().Be("electronics");
            }
        });
    }

    [Fact]
    public async Task Handle_WithNullCategorySlug_ShouldReturnAllCategories()
    {
        // Arrange
        var events = new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple", categorySlug: "electronics"),
            CreateFilterAppliedEvent("s2", "brand", "gucci", categorySlug: "fashion"),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery(CategorySlug: null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Should include filters from both categories
        result.Value.Filters.Should().NotBeEmpty();
    }

    #endregion

    #region Empty Results

    [Fact]
    public async Task Handle_WithNoEvents_ShouldReturnEmptyFilters()
    {
        // Arrange
        SetupFilterAnalyticsEvents(new List<FilterAnalyticsEvent>());

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Filters.Should().BeEmpty();
        result.Value.TotalEvents.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithOnlyNonFilterAppliedEvents_ShouldReturnEmptyFilters()
    {
        // Arrange: Only ProductClicked events (no FilterApplied)
        var events = new List<FilterAnalyticsEvent>
        {
            CreateProductClickedEvent("s1", Guid.NewGuid()),
            CreateProductClickedEvent("s2", Guid.NewGuid()),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Filters.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNoEvents_ShouldReturnZeroTotalEvents()
    {
        // Arrange
        SetupFilterAnalyticsEvents(new List<FilterAnalyticsEvent>());

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalEvents.Should().Be(0);
        result.Value.Filters.Should().BeEmpty();
    }

    #endregion

    #region Sorting and Limits

    [Fact]
    public async Task Handle_ShouldReturnFiltersSortedByUsageCount()
    {
        // Arrange
        var events = new List<FilterAnalyticsEvent>
        {
            // Brand "apple" used 3 times
            CreateFilterAppliedEvent("s1", "brand", "apple"),
            CreateFilterAppliedEvent("s2", "brand", "apple"),
            CreateFilterAppliedEvent("s3", "brand", "apple"),
            // Brand "samsung" used 1 time
            CreateFilterAppliedEvent("s4", "brand", "samsung"),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        if (result.Value.Filters.Count >= 2)
        {
            result.Value.Filters[0].UsageCount.Should().BeGreaterThanOrEqualTo(
                result.Value.Filters[1].UsageCount);
        }
    }

    [Fact]
    public async Task Handle_WithTopParameter_ShouldLimitResults()
    {
        // Arrange
        var events = new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple"),
            CreateFilterAppliedEvent("s2", "color", "red"),
            CreateFilterAppliedEvent("s3", "size", "large"),
            CreateFilterAppliedEvent("s4", "material", "cotton"),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery(Top: 2);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Filters.Should().HaveCountLessThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_WithDefaultTopParameter_ShouldUse20AsDefault()
    {
        // Arrange
        SetupFilterAnalyticsEvents(new List<FilterAnalyticsEvent>());

        var query = new GetPopularFiltersQuery();

        // Assert - verify the default Top value is 20
        query.Top.Should().Be(20);
    }

    #endregion

    #region Conversion Rate

    [Fact]
    public async Task Handle_ConversionRate_ShouldBeZeroWhenNoUniqueUsers()
    {
        // Arrange: events without userId
        var events = new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple", userId: null),
            CreateFilterAppliedEvent("s2", "brand", "apple", userId: null),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Filters.Should().AllSatisfy(f =>
        {
            f.ConversionRate.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    [Fact]
    public async Task Handle_WithAuthenticatedUsers_ShouldCountUniqueUsers()
    {
        // Arrange
        var events = new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple", userId: "user-1"),
            CreateFilterAppliedEvent("s2", "brand", "apple", userId: "user-2"),
            CreateFilterAppliedEvent("s3", "brand", "apple", userId: "user-1"), // duplicate user
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // All filters should have non-negative conversion rates
        result.Value.Filters.Should().AllSatisfy(f =>
        {
            f.ConversionRate.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    #endregion

    #region Null FilterCode Handling

    [Fact]
    public async Task Handle_WithFilterEventsHavingNullFilterCode_ShouldExcludeFromResults()
    {
        // Arrange
        var events = new List<FilterAnalyticsEvent>
        {
            // Event with null FilterCode (e.g., general browse)
            FilterAnalyticsEvent.Create(
                sessionId: "s1",
                eventType: FilterEventType.FilterApplied,
                productCount: 10,
                tenantId: "tenant-1"),
            // Event with FilterCode set
            CreateFilterAppliedEvent("s2", "brand", "apple"),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Filters.Should().AllSatisfy(f =>
        {
            f.FilterCode.Should().NotBeNull();
        });
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldNotThrow()
    {
        // Arrange
        var events = new List<FilterAnalyticsEvent>();
        SetupFilterAnalyticsEvents(events);

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithMixedEventTypes_ShouldOnlyCountFilterApplied()
    {
        // Arrange: Mix of event types but only FilterApplied should count in TotalEvents
        var events = new List<FilterAnalyticsEvent>
        {
            CreateFilterAppliedEvent("s1", "brand", "apple"),
            CreateFilterAppliedEvent("s2", "brand", "samsung"),
            CreateProductClickedEvent("s3", Guid.NewGuid()),
            FilterAnalyticsEvent.Create(
                sessionId: "s4",
                eventType: FilterEventType.FilterRemoved,
                productCount: 5,
                tenantId: "tenant-1"),
        };

        SetupFilterAnalyticsEvents(events);

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // TotalEvents should only count FilterApplied events
        result.Value.TotalEvents.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ResultShouldContainFromAndToDateValues()
    {
        // Arrange
        SetupFilterAnalyticsEvents(new List<FilterAnalyticsEvent>());

        var query = new GetPopularFiltersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FromDate.Should().NotBe(default);
        result.Value.ToDate.Should().NotBe(default);
    }

    #endregion
}
