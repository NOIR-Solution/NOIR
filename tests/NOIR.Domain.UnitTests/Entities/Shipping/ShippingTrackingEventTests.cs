using NOIR.Domain.Entities.Shipping;

namespace NOIR.Domain.UnitTests.Entities.Shipping;

/// <summary>
/// Unit tests for the ShippingTrackingEvent entity.
/// Tests factory method, property initialization, and various field combinations.
/// </summary>
public class ShippingTrackingEventTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestShippingOrderId = Guid.NewGuid();
    private const string TestEventType = "PICKED_UP";
    private const ShippingStatus TestStatus = ShippingStatus.PickedUp;
    private const string TestDescription = "Package picked up from sender warehouse";
    private const string TestLocation = "Ho Chi Minh City - Warehouse A";

    /// <summary>
    /// Helper to create a default valid tracking event for tests.
    /// </summary>
    private static ShippingTrackingEvent CreateTestTrackingEvent(
        Guid? shippingOrderId = null,
        string eventType = TestEventType,
        ShippingStatus status = TestStatus,
        string description = TestDescription,
        string? location = TestLocation,
        DateTimeOffset? eventDate = null,
        string? rawPayload = null,
        string? tenantId = TestTenantId)
    {
        return ShippingTrackingEvent.Create(
            shippingOrderId ?? TestShippingOrderId,
            eventType,
            status,
            description,
            location,
            eventDate ?? DateTimeOffset.UtcNow,
            rawPayload,
            tenantId);
    }

    #region Create Factory Method

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidEvent()
    {
        // Arrange
        var eventDate = DateTimeOffset.UtcNow.AddMinutes(-10);

        // Act
        var trackingEvent = ShippingTrackingEvent.Create(
            TestShippingOrderId,
            TestEventType,
            TestStatus,
            TestDescription,
            TestLocation,
            eventDate,
            null,
            TestTenantId);

        // Assert
        trackingEvent.Should().NotBeNull();
        trackingEvent.Id.Should().NotBe(Guid.Empty);
        trackingEvent.ShippingOrderId.Should().Be(TestShippingOrderId);
        trackingEvent.EventType.Should().Be(TestEventType);
        trackingEvent.Status.Should().Be(TestStatus);
        trackingEvent.Description.Should().Be(TestDescription);
        trackingEvent.Location.Should().Be(TestLocation);
        trackingEvent.EventDate.Should().Be(eventDate);
        trackingEvent.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetReceivedAtToCurrentUtcTime()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var trackingEvent = CreateTestTrackingEvent();

        // Assert
        trackingEvent.ReceivedAt.Should().BeOnOrAfter(beforeCreate);
        trackingEvent.ReceivedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_WithNullLocation_ShouldAllowNull()
    {
        // Act
        var trackingEvent = CreateTestTrackingEvent(location: null);

        // Assert
        trackingEvent.Location.Should().BeNull();
    }

    [Fact]
    public void Create_WithRawPayload_ShouldSetPayload()
    {
        // Arrange
        var rawPayload = """{"status":"picked_up","timestamp":"2026-02-19T10:00:00Z"}""";

        // Act
        var trackingEvent = CreateTestTrackingEvent(rawPayload: rawPayload);

        // Assert
        trackingEvent.RawPayload.Should().Be(rawPayload);
    }

    [Fact]
    public void Create_WithNullRawPayload_ShouldAllowNull()
    {
        // Act
        var trackingEvent = CreateTestTrackingEvent(rawPayload: null);

        // Assert
        trackingEvent.RawPayload.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var trackingEvent = CreateTestTrackingEvent(tenantId: null);

        // Assert
        trackingEvent.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleEvents_ShouldGenerateUniqueIds()
    {
        // Act
        var event1 = CreateTestTrackingEvent();
        var event2 = CreateTestTrackingEvent();

        // Assert
        event1.Id.Should().NotBe(event2.Id);
    }

    #endregion

    #region Status Values

    [Theory]
    [InlineData(ShippingStatus.Draft)]
    [InlineData(ShippingStatus.AwaitingPickup)]
    [InlineData(ShippingStatus.PickedUp)]
    [InlineData(ShippingStatus.InTransit)]
    [InlineData(ShippingStatus.OutForDelivery)]
    [InlineData(ShippingStatus.Delivered)]
    [InlineData(ShippingStatus.DeliveryFailed)]
    [InlineData(ShippingStatus.Cancelled)]
    [InlineData(ShippingStatus.Returning)]
    [InlineData(ShippingStatus.Returned)]
    public void Create_WithDifferentStatuses_ShouldSetCorrectStatus(ShippingStatus status)
    {
        // Act
        var trackingEvent = CreateTestTrackingEvent(status: status);

        // Assert
        trackingEvent.Status.Should().Be(status);
    }

    #endregion

    #region EventType Variations

    [Theory]
    [InlineData("PICKED_UP")]
    [InlineData("IN_TRANSIT")]
    [InlineData("OUT_FOR_DELIVERY")]
    [InlineData("DELIVERED")]
    [InlineData("DELIVERY_FAILED")]
    [InlineData("RETURNING")]
    [InlineData("RETURNED")]
    public void Create_WithDifferentEventTypes_ShouldSetCorrectEventType(string eventType)
    {
        // Act
        var trackingEvent = CreateTestTrackingEvent(eventType: eventType);

        // Assert
        trackingEvent.EventType.Should().Be(eventType);
    }

    #endregion

    #region EventDate

    [Fact]
    public void Create_WithPastEventDate_ShouldSetCorrectDate()
    {
        // Arrange
        var pastDate = DateTimeOffset.UtcNow.AddHours(-6);

        // Act
        var trackingEvent = CreateTestTrackingEvent(eventDate: pastDate);

        // Assert
        trackingEvent.EventDate.Should().Be(pastDate);
    }

    [Fact]
    public void Create_EventDateShouldDifferFromReceivedAt()
    {
        // Arrange - event happened 2 hours ago but we just received it
        var eventDate = DateTimeOffset.UtcNow.AddHours(-2);

        // Act
        var trackingEvent = CreateTestTrackingEvent(eventDate: eventDate);

        // Assert
        trackingEvent.EventDate.Should().BeBefore(trackingEvent.ReceivedAt);
    }

    [Fact]
    public void Create_WithFutureEventDate_ShouldStillSetDate()
    {
        // Arrange - some providers may send future dates (e.g., estimated delivery)
        var futureDate = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var trackingEvent = CreateTestTrackingEvent(eventDate: futureDate);

        // Assert
        trackingEvent.EventDate.Should().Be(futureDate);
    }

    #endregion

    #region Integration with ShippingOrder

    [Fact]
    public void Create_ShouldBeAddableToShippingOrder()
    {
        // Arrange
        var shippingOrder = ShippingOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            ShippingProviderCode.GHTK, "EXPRESS", "Express",
            "{}", "{}", "{}", "{}", "[]",
            500m, 1_000_000m, null, false, null, TestTenantId);
        shippingOrder.SetProviderResponse("TRK-001", null, null, null, 20_000m, 0m, 0m, null, null);

        var trackingEvent = ShippingTrackingEvent.Create(
            shippingOrder.Id, "IN_TRANSIT", ShippingStatus.InTransit,
            "Package moving", "Hub B", DateTimeOffset.UtcNow, null, TestTenantId);

        // Act
        shippingOrder.AddTrackingEvent(trackingEvent);

        // Assert
        shippingOrder.TrackingEvents.Should().Contain(trackingEvent);
        shippingOrder.Status.Should().Be(ShippingStatus.InTransit);
    }

    [Fact]
    public void Create_WithLargeRawPayload_ShouldStoreCompletely()
    {
        // Arrange - simulate a large webhook payload
        var largePayload = new string('x', 10_000);

        // Act
        var trackingEvent = CreateTestTrackingEvent(rawPayload: largePayload);

        // Assert
        trackingEvent.RawPayload.Should().HaveLength(10_000);
    }

    #endregion
}
