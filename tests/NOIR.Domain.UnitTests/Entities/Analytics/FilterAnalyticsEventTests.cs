using NOIR.Domain.Entities.Analytics;

namespace NOIR.Domain.UnitTests.Entities.Analytics;

/// <summary>
/// Unit tests for the FilterAnalyticsEvent entity.
/// Tests the generic Create factory, specialized factory methods
/// (FilterApplied, SearchPerformed, ProductClicked), and property initialization.
/// </summary>
public class FilterAnalyticsEventTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestSessionId = "session-abc-123";

    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidEvent()
    {
        // Act
        var ev = FilterAnalyticsEvent.Create(
            TestSessionId, FilterEventType.FilterApplied, 42, TestTenantId);

        // Assert
        ev.Should().NotBeNull();
        ev.Id.Should().NotBe(Guid.Empty);
        ev.SessionId.Should().Be(TestSessionId);
        ev.EventType.Should().Be(FilterEventType.FilterApplied);
        ev.ProductCount.Should().Be(42);
        ev.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldDefaultOptionalToNull()
    {
        // Act
        var ev = FilterAnalyticsEvent.Create(TestSessionId, FilterEventType.FilterApplied, 0);

        // Assert
        ev.UserId.Should().BeNull();
        ev.CategorySlug.Should().BeNull();
        ev.FilterCode.Should().BeNull();
        ev.FilterValue.Should().BeNull();
        ev.SearchQuery.Should().BeNull();
        ev.ClickedProductId.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var clickedProductId = Guid.NewGuid();

        // Act
        var ev = FilterAnalyticsEvent.Create(
            sessionId: TestSessionId,
            eventType: FilterEventType.FilterApplied,
            productCount: 25,
            tenantId: TestTenantId,
            userId: "user-456",
            categorySlug: "electronics",
            filterCode: "brand",
            filterValue: "apple",
            searchQuery: "iphone",
            clickedProductId: clickedProductId);

        // Assert
        ev.SessionId.Should().Be(TestSessionId);
        ev.EventType.Should().Be(FilterEventType.FilterApplied);
        ev.ProductCount.Should().Be(25);
        ev.UserId.Should().Be("user-456");
        ev.CategorySlug.Should().Be("electronics");
        ev.FilterCode.Should().Be("brand");
        ev.FilterValue.Should().Be("apple");
        ev.SearchQuery.Should().Be("iphone");
        ev.ClickedProductId.Should().Be(clickedProductId);
        ev.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var ev = FilterAnalyticsEvent.Create(TestSessionId, FilterEventType.FilterApplied, 0, null);

        // Assert
        ev.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var ev1 = FilterAnalyticsEvent.Create(TestSessionId, FilterEventType.FilterApplied, 0);
        var ev2 = FilterAnalyticsEvent.Create(TestSessionId, FilterEventType.FilterApplied, 0);

        // Assert
        ev1.Id.Should().NotBe(ev2.Id);
    }

    [Fact]
    public void Create_WithZeroProductCount_ShouldSucceed()
    {
        // Act
        var ev = FilterAnalyticsEvent.Create(TestSessionId, FilterEventType.FilterApplied, 0);

        // Assert
        ev.ProductCount.Should().Be(0);
    }

    #endregion

    #region FilterApplied Factory Tests

    [Fact]
    public void FilterApplied_ShouldSetCorrectEventType()
    {
        // Act
        var ev = FilterAnalyticsEvent.FilterApplied(
            TestSessionId, "brand", "apple", 15, TestTenantId);

        // Assert
        ev.EventType.Should().Be(FilterEventType.FilterApplied);
    }

    [Fact]
    public void FilterApplied_ShouldSetFilterCodeAndValue()
    {
        // Act
        var ev = FilterAnalyticsEvent.FilterApplied(
            TestSessionId, "color", "red", 10);

        // Assert
        ev.FilterCode.Should().Be("color");
        ev.FilterValue.Should().Be("red");
    }

    [Fact]
    public void FilterApplied_WithCategorySlug_ShouldSetCategory()
    {
        // Act
        var ev = FilterAnalyticsEvent.FilterApplied(
            TestSessionId, "price", "100-500", 8,
            categorySlug: "clothing");

        // Assert
        ev.CategorySlug.Should().Be("clothing");
    }

    [Fact]
    public void FilterApplied_WithUserId_ShouldSetUserId()
    {
        // Act
        var ev = FilterAnalyticsEvent.FilterApplied(
            TestSessionId, "size", "M", 20,
            userId: "auth-user-789");

        // Assert
        ev.UserId.Should().Be("auth-user-789");
    }

    [Fact]
    public void FilterApplied_ShouldSetProductCount()
    {
        // Act
        var ev = FilterAnalyticsEvent.FilterApplied(
            TestSessionId, "brand", "nike", 150);

        // Assert
        ev.ProductCount.Should().Be(150);
    }

    [Fact]
    public void FilterApplied_ShouldNotSetSearchQueryOrClickedProduct()
    {
        // Act
        var ev = FilterAnalyticsEvent.FilterApplied(
            TestSessionId, "brand", "adidas", 50);

        // Assert
        ev.SearchQuery.Should().BeNull();
        ev.ClickedProductId.Should().BeNull();
    }

    #endregion

    #region SearchPerformed Factory Tests

    [Fact]
    public void SearchPerformed_ShouldSetCorrectEventType()
    {
        // Act
        var ev = FilterAnalyticsEvent.SearchPerformed(
            TestSessionId, "wireless headphones", 35, TestTenantId);

        // Assert
        ev.EventType.Should().Be(FilterEventType.SearchPerformed);
    }

    [Fact]
    public void SearchPerformed_ShouldSetSearchQuery()
    {
        // Act
        var ev = FilterAnalyticsEvent.SearchPerformed(
            TestSessionId, "blue running shoes", 12);

        // Assert
        ev.SearchQuery.Should().Be("blue running shoes");
    }

    [Fact]
    public void SearchPerformed_ShouldSetProductCount()
    {
        // Act
        var ev = FilterAnalyticsEvent.SearchPerformed(
            TestSessionId, "laptop", 200);

        // Assert
        ev.ProductCount.Should().Be(200);
    }

    [Fact]
    public void SearchPerformed_WithCategorySlug_ShouldSetCategory()
    {
        // Act
        var ev = FilterAnalyticsEvent.SearchPerformed(
            TestSessionId, "iphone case", 25,
            categorySlug: "accessories");

        // Assert
        ev.CategorySlug.Should().Be("accessories");
    }

    [Fact]
    public void SearchPerformed_ShouldNotSetFilterCodeOrClickedProduct()
    {
        // Act
        var ev = FilterAnalyticsEvent.SearchPerformed(
            TestSessionId, "test query", 10);

        // Assert
        ev.FilterCode.Should().BeNull();
        ev.FilterValue.Should().BeNull();
        ev.ClickedProductId.Should().BeNull();
    }

    #endregion

    #region ProductClicked Factory Tests

    [Fact]
    public void ProductClicked_ShouldSetCorrectEventType()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var ev = FilterAnalyticsEvent.ProductClicked(
            TestSessionId, productId, TestTenantId);

        // Assert
        ev.EventType.Should().Be(FilterEventType.ProductClicked);
    }

    [Fact]
    public void ProductClicked_ShouldSetClickedProductId()
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var ev = FilterAnalyticsEvent.ProductClicked(TestSessionId, productId);

        // Assert
        ev.ClickedProductId.Should().Be(productId);
    }

    [Fact]
    public void ProductClicked_ShouldSetProductCountToZero()
    {
        // Act
        var ev = FilterAnalyticsEvent.ProductClicked(TestSessionId, Guid.NewGuid());

        // Assert
        ev.ProductCount.Should().Be(0);
    }

    [Fact]
    public void ProductClicked_WithCategorySlug_ShouldSetCategory()
    {
        // Act
        var ev = FilterAnalyticsEvent.ProductClicked(
            TestSessionId, Guid.NewGuid(),
            categorySlug: "electronics");

        // Assert
        ev.CategorySlug.Should().Be("electronics");
    }

    [Fact]
    public void ProductClicked_ShouldNotSetFilterOrSearchFields()
    {
        // Act
        var ev = FilterAnalyticsEvent.ProductClicked(TestSessionId, Guid.NewGuid());

        // Assert
        ev.FilterCode.Should().BeNull();
        ev.FilterValue.Should().BeNull();
        ev.SearchQuery.Should().BeNull();
    }

    #endregion

    #region Event Type Coverage

    [Theory]
    [InlineData(FilterEventType.FilterApplied)]
    [InlineData(FilterEventType.FilterRemoved)]
    [InlineData(FilterEventType.SearchPerformed)]
    [InlineData(FilterEventType.ProductClicked)]
    public void Create_WithAllEventTypes_ShouldSetCorrectType(FilterEventType eventType)
    {
        // Act
        var ev = FilterAnalyticsEvent.Create(TestSessionId, eventType, 0);

        // Assert
        ev.EventType.Should().Be(eventType);
    }

    #endregion
}
