using NOIR.Domain.Entities.Shipping;
using NOIR.Domain.Events.Shipping;

namespace NOIR.Domain.UnitTests.Entities.Shipping;

/// <summary>
/// Unit tests for the ShippingOrder aggregate root entity.
/// Tests factory methods, state transitions, fee calculations,
/// tracking events, cancellation, and domain event raising.
/// </summary>
public class ShippingOrderTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestProviderId = Guid.NewGuid();
    private const ShippingProviderCode TestProviderCode = ShippingProviderCode.GHTK;
    private const string TestServiceTypeCode = "EXPRESS";
    private const string TestServiceTypeName = "Express Delivery";
    private const string TestPickupAddressJson = """{"city":"Ho Chi Minh"}""";
    private const string TestDeliveryAddressJson = """{"city":"Ha Noi"}""";
    private const string TestSenderJson = """{"name":"Sender A","phone":"0901234567"}""";
    private const string TestRecipientJson = """{"name":"Recipient B","phone":"0909876543"}""";
    private const string TestItemsJson = """[{"name":"Product A","qty":2}]""";
    private const decimal TestWeightGrams = 500m;
    private const decimal TestDeclaredValue = 1_000_000m;

    /// <summary>
    /// Helper to create a default valid shipping order for tests.
    /// </summary>
    private static ShippingOrder CreateTestShippingOrder(
        Guid? orderId = null,
        Guid? providerId = null,
        ShippingProviderCode providerCode = TestProviderCode,
        string serviceTypeCode = TestServiceTypeCode,
        string serviceTypeName = TestServiceTypeName,
        string pickupAddressJson = TestPickupAddressJson,
        string deliveryAddressJson = TestDeliveryAddressJson,
        string senderJson = TestSenderJson,
        string recipientJson = TestRecipientJson,
        string itemsJson = TestItemsJson,
        decimal weightGrams = TestWeightGrams,
        decimal declaredValue = TestDeclaredValue,
        decimal? codAmount = null,
        bool isFreeship = false,
        string? notes = null,
        string? tenantId = TestTenantId)
    {
        return ShippingOrder.Create(
            orderId ?? TestOrderId,
            providerId ?? TestProviderId,
            providerCode,
            serviceTypeCode,
            serviceTypeName,
            pickupAddressJson,
            deliveryAddressJson,
            senderJson,
            recipientJson,
            itemsJson,
            weightGrams,
            declaredValue,
            codAmount,
            isFreeship,
            notes,
            tenantId);
    }

    /// <summary>
    /// Helper to create a shipping order and submit it to the provider (AwaitingPickup status).
    /// </summary>
    private static ShippingOrder CreateSubmittedShippingOrder(
        string trackingNumber = "TRK-001",
        decimal baseRate = 30_000m,
        decimal codFee = 5_000m,
        decimal insuranceFee = 2_000m)
    {
        var order = CreateTestShippingOrder();
        order.SetProviderResponse(
            trackingNumber,
            "PROVIDER-ORD-123",
            "https://label.example.com/label.pdf",
            "https://track.example.com/TRK-001",
            baseRate,
            codFee,
            insuranceFee,
            DateTimeOffset.UtcNow.AddDays(3),
            """{"status":"success"}""");
        return order;
    }

    #region Create Factory Method

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidShippingOrder()
    {
        // Act
        var order = CreateTestShippingOrder();

        // Assert
        order.ShouldNotBeNull();
        order.Id.ShouldNotBe(Guid.Empty);
        order.OrderId.ShouldBe(TestOrderId);
        order.ProviderId.ShouldBe(TestProviderId);
        order.ProviderCode.ShouldBe(TestProviderCode);
        order.ServiceTypeCode.ShouldBe(TestServiceTypeCode);
        order.ServiceTypeName.ShouldBe(TestServiceTypeName);
        order.PickupAddressJson.ShouldBe(TestPickupAddressJson);
        order.DeliveryAddressJson.ShouldBe(TestDeliveryAddressJson);
        order.SenderJson.ShouldBe(TestSenderJson);
        order.RecipientJson.ShouldBe(TestRecipientJson);
        order.ItemsJson.ShouldBe(TestItemsJson);
        order.WeightGrams.ShouldBe(TestWeightGrams);
        order.DeclaredValue.ShouldBe(TestDeclaredValue);
        order.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetStatusToDraft()
    {
        // Act
        var order = CreateTestShippingOrder();

        // Assert
        order.Status.ShouldBe(ShippingStatus.Draft);
    }

    [Fact]
    public void Create_ShouldInitializeFinancialDefaults()
    {
        // Act
        var order = CreateTestShippingOrder();

        // Assert
        order.BaseRate.ShouldBe(0);
        order.CodFee.ShouldBe(0);
        order.InsuranceFee.ShouldBe(0);
        order.TotalShippingFee.ShouldBe(0);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var order = CreateTestShippingOrder();

        // Assert
        order.ProviderOrderId.ShouldBeNull();
        order.LabelUrl.ShouldBeNull();
        order.TrackingUrl.ShouldBeNull();
        order.EstimatedDeliveryDate.ShouldBeNull();
        order.ActualDeliveryDate.ShouldBeNull();
        order.PickedUpAt.ShouldBeNull();
        order.ProviderRawResponse.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldInitializeTrackingNumberToEmpty()
    {
        // Act
        var order = CreateTestShippingOrder();

        // Assert
        order.TrackingNumber.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyTrackingEventsCollection()
    {
        // Act
        var order = CreateTestShippingOrder();

        // Assert
        order.TrackingEvents.ShouldNotBeNull();
        order.TrackingEvents.ShouldBeEmpty();
    }

    [Fact]
    public void Create_WithCodAmount_ShouldSetCodAmount()
    {
        // Act
        var order = CreateTestShippingOrder(codAmount: 500_000m);

        // Assert
        order.CodAmount.ShouldBe(500_000m);
    }

    [Fact]
    public void Create_WithoutCodAmount_ShouldHaveNullCodAmount()
    {
        // Act
        var order = CreateTestShippingOrder(codAmount: null);

        // Assert
        order.CodAmount.ShouldBeNull();
    }

    [Fact]
    public void Create_WithFreeship_ShouldSetFreeshipFlag()
    {
        // Act
        var order = CreateTestShippingOrder(isFreeship: true);

        // Assert
        order.IsFreeship.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithoutFreeship_ShouldDefaultToFalse()
    {
        // Act
        var order = CreateTestShippingOrder(isFreeship: false);

        // Assert
        order.IsFreeship.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNotes_ShouldSetNotes()
    {
        // Act
        var order = CreateTestShippingOrder(notes: "Handle with care");

        // Assert
        order.Notes.ShouldBe("Handle with care");
    }

    [Fact]
    public void Create_WithoutNotes_ShouldHaveNullNotes()
    {
        // Act
        var order = CreateTestShippingOrder(notes: null);

        // Assert
        order.Notes.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var order = CreateTestShippingOrder(tenantId: null);

        // Assert
        order.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldRaiseShippingOrderCreatedEvent()
    {
        // Act
        var order = CreateTestShippingOrder();

        // Assert
        var __evt = order.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<ShippingOrderCreatedEvent>();

        __evt.ShippingOrderId.ShouldBe(order.Id);

        __evt.OrderId.ShouldBe(TestOrderId);

        __evt.ProviderCode.ShouldBe(TestProviderCode);
    }

    [Theory]
    [InlineData(ShippingProviderCode.GHTK)]
    [InlineData(ShippingProviderCode.GHN)]
    [InlineData(ShippingProviderCode.JTExpress)]
    [InlineData(ShippingProviderCode.ViettelPost)]
    [InlineData(ShippingProviderCode.NinjaVan)]
    [InlineData(ShippingProviderCode.VNPost)]
    [InlineData(ShippingProviderCode.BestExpress)]
    [InlineData(ShippingProviderCode.Custom)]
    public void Create_WithDifferentProviderCodes_ShouldSetCorrectCode(ShippingProviderCode code)
    {
        // Act
        var order = CreateTestShippingOrder(providerCode: code);

        // Assert
        order.ProviderCode.ShouldBe(code);
    }

    [Fact]
    public void Create_MultipleOrders_ShouldGenerateUniqueIds()
    {
        // Act
        var order1 = CreateTestShippingOrder();
        var order2 = CreateTestShippingOrder();

        // Assert
        order1.Id.ShouldNotBe(order2.Id);
    }

    #endregion

    #region SetProviderResponse

    [Fact]
    public void SetProviderResponse_ShouldSetAllResponseFields()
    {
        // Arrange
        var order = CreateTestShippingOrder();
        var estimatedDelivery = DateTimeOffset.UtcNow.AddDays(3);

        // Act
        order.SetProviderResponse(
            "TRK-12345",
            "PROV-ORD-001",
            "https://label.example.com/label.pdf",
            "https://track.example.com/TRK-12345",
            30_000m,
            5_000m,
            2_000m,
            estimatedDelivery,
            """{"status":"created"}""");

        // Assert
        order.TrackingNumber.ShouldBe("TRK-12345");
        order.ProviderOrderId.ShouldBe("PROV-ORD-001");
        order.LabelUrl.ShouldBe("https://label.example.com/label.pdf");
        order.TrackingUrl.ShouldBe("https://track.example.com/TRK-12345");
        order.BaseRate.ShouldBe(30_000m);
        order.CodFee.ShouldBe(5_000m);
        order.InsuranceFee.ShouldBe(2_000m);
        order.EstimatedDeliveryDate.ShouldBe(estimatedDelivery);
        order.ProviderRawResponse.ShouldBe("""{"status":"created"}""");
    }

    [Fact]
    public void SetProviderResponse_ShouldCalculateTotalShippingFee()
    {
        // Arrange
        var order = CreateTestShippingOrder();

        // Act
        order.SetProviderResponse("TRK-001", null, null, null,
            baseRate: 30_000m, codFee: 5_000m, insuranceFee: 2_000m,
            estimatedDeliveryDate: null, rawResponse: null);

        // Assert - TotalShippingFee = BaseRate + CodFee + InsuranceFee
        order.TotalShippingFee.ShouldBe(37_000m);
    }

    [Fact]
    public void SetProviderResponse_WithZeroFees_ShouldHaveZeroTotal()
    {
        // Arrange
        var order = CreateTestShippingOrder();

        // Act
        order.SetProviderResponse("TRK-001", null, null, null,
            baseRate: 0m, codFee: 0m, insuranceFee: 0m,
            estimatedDeliveryDate: null, rawResponse: null);

        // Assert
        order.TotalShippingFee.ShouldBe(0m);
    }

    [Fact]
    public void SetProviderResponse_ShouldTransitionStatusToAwaitingPickup()
    {
        // Arrange
        var order = CreateTestShippingOrder();
        order.Status.ShouldBe(ShippingStatus.Draft);

        // Act
        order.SetProviderResponse("TRK-001", null, null, null,
            25_000m, 0m, 0m, null, null);

        // Assert
        order.Status.ShouldBe(ShippingStatus.AwaitingPickup);
    }

    [Fact]
    public void SetProviderResponse_ShouldRaiseShippingOrderSubmittedEvent()
    {
        // Arrange
        var order = CreateTestShippingOrder();
        order.ClearDomainEvents();

        // Act
        order.SetProviderResponse("TRK-SUBMIT", null, null, null,
            25_000m, 0m, 0m, null, null);

        // Assert
        var __evt = order.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<ShippingOrderSubmittedEvent>();

        __evt.ShippingOrderId.ShouldBe(order.Id);

        __evt.TrackingNumber.ShouldBe("TRK-SUBMIT");

        __evt.ProviderCode.ShouldBe(TestProviderCode);
    }

    [Fact]
    public void SetProviderResponse_WithNullOptionalFields_ShouldAllowNulls()
    {
        // Arrange
        var order = CreateTestShippingOrder();

        // Act
        order.SetProviderResponse("TRK-001",
            providerOrderId: null,
            labelUrl: null,
            trackingUrl: null,
            baseRate: 20_000m,
            codFee: 0m,
            insuranceFee: 0m,
            estimatedDeliveryDate: null,
            rawResponse: null);

        // Assert
        order.ProviderOrderId.ShouldBeNull();
        order.LabelUrl.ShouldBeNull();
        order.TrackingUrl.ShouldBeNull();
        order.EstimatedDeliveryDate.ShouldBeNull();
        order.ProviderRawResponse.ShouldBeNull();
    }

    #endregion

    #region UpdateStatus

    [Fact]
    public void UpdateStatus_ToPickedUp_ShouldSetPickedUpAt()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        order.UpdateStatus(ShippingStatus.PickedUp, "Warehouse A");

        // Assert
        order.Status.ShouldBe(ShippingStatus.PickedUp);
        order.PickedUpAt.ShouldNotBeNull();
        order.PickedUpAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
    }

    [Fact]
    public void UpdateStatus_ToDelivered_ShouldSetActualDeliveryDate()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        order.UpdateStatus(ShippingStatus.Delivered, "Customer address");

        // Assert
        order.Status.ShouldBe(ShippingStatus.Delivered);
        order.ActualDeliveryDate.ShouldNotBeNull();
        order.ActualDeliveryDate!.Value.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
    }

    [Fact]
    public void UpdateStatus_ToInTransit_ShouldNotSetSpecialTimestamps()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();

        // Act
        order.UpdateStatus(ShippingStatus.InTransit, "Distribution center");

        // Assert
        order.Status.ShouldBe(ShippingStatus.InTransit);
        order.PickedUpAt.ShouldBeNull();
        order.ActualDeliveryDate.ShouldBeNull();
    }

    [Fact]
    public void UpdateStatus_ShouldRaiseShippingOrderStatusChangedEvent()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder(trackingNumber: "TRK-EVT");
        order.ClearDomainEvents();

        // Act
        order.UpdateStatus(ShippingStatus.InTransit, "Hub B");

        // Assert
        var __evt = order.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<ShippingOrderStatusChangedEvent>();

        __evt.ShippingOrderId.ShouldBe(order.Id);

        __evt.TrackingNumber.ShouldBe("TRK-EVT");

        __evt.PreviousStatus.ShouldBe(ShippingStatus.AwaitingPickup);

        __evt.NewStatus.ShouldBe(ShippingStatus.InTransit);

        __evt.Location.ShouldBe("Hub B");
    }

    [Fact]
    public void UpdateStatus_WithNullLocation_ShouldRaiseEventWithNullLocation()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        order.ClearDomainEvents();

        // Act
        order.UpdateStatus(ShippingStatus.InTransit);

        // Assert
        order.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<ShippingOrderStatusChangedEvent>()
            .Location.ShouldBeNull();
    }

    [Theory]
    [InlineData(ShippingStatus.OutForDelivery)]
    [InlineData(ShippingStatus.DeliveryFailed)]
    [InlineData(ShippingStatus.Returning)]
    [InlineData(ShippingStatus.Returned)]
    public void UpdateStatus_ToVariousStatuses_ShouldUpdateStatus(ShippingStatus targetStatus)
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();

        // Act
        order.UpdateStatus(targetStatus);

        // Assert
        order.Status.ShouldBe(targetStatus);
    }

    #endregion

    #region AddTrackingEvent

    [Fact]
    public void AddTrackingEvent_ShouldAddEventToCollection()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        var trackingEvent = ShippingTrackingEvent.Create(
            order.Id, "PICKED_UP", ShippingStatus.PickedUp,
            "Package picked up from sender", "Warehouse A",
            DateTimeOffset.UtcNow, null, TestTenantId);

        // Act
        order.AddTrackingEvent(trackingEvent);

        // Assert
        order.TrackingEvents.Count().ShouldBe(1);
        order.TrackingEvents.ShouldContain(trackingEvent);
    }

    [Fact]
    public void AddTrackingEvent_WithDifferentStatus_ShouldUpdateOrderStatus()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        order.Status.ShouldBe(ShippingStatus.AwaitingPickup);
        var trackingEvent = ShippingTrackingEvent.Create(
            order.Id, "IN_TRANSIT", ShippingStatus.InTransit,
            "Package in transit", "Hub B",
            DateTimeOffset.UtcNow, null, TestTenantId);

        // Act
        order.AddTrackingEvent(trackingEvent);

        // Assert
        order.Status.ShouldBe(ShippingStatus.InTransit);
    }

    [Fact]
    public void AddTrackingEvent_WithSameStatus_ShouldNotRaiseStatusChangedEvent()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        order.ClearDomainEvents();
        var trackingEvent = ShippingTrackingEvent.Create(
            order.Id, "AWAITING_PICKUP_UPDATE", ShippingStatus.AwaitingPickup,
            "Waiting for courier", "Warehouse",
            DateTimeOffset.UtcNow, null, TestTenantId);

        // Act
        order.AddTrackingEvent(trackingEvent);

        // Assert - no status change event raised because status is the same
        order.DomainEvents.ShouldBeEmpty();
        order.TrackingEvents.Count().ShouldBe(1);
    }

    [Fact]
    public void AddTrackingEvent_MultipleEvents_ShouldTrackAll()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        var event1 = ShippingTrackingEvent.Create(
            order.Id, "PICKED_UP", ShippingStatus.PickedUp,
            "Picked up", "Warehouse", DateTimeOffset.UtcNow);
        var event2 = ShippingTrackingEvent.Create(
            order.Id, "IN_TRANSIT", ShippingStatus.InTransit,
            "In transit", "Hub", DateTimeOffset.UtcNow);
        var event3 = ShippingTrackingEvent.Create(
            order.Id, "DELIVERED", ShippingStatus.Delivered,
            "Delivered", "Customer home", DateTimeOffset.UtcNow);

        // Act
        order.AddTrackingEvent(event1);
        order.AddTrackingEvent(event2);
        order.AddTrackingEvent(event3);

        // Assert
        order.TrackingEvents.Count().ShouldBe(3);
        order.Status.ShouldBe(ShippingStatus.Delivered);
        order.ActualDeliveryDate.ShouldNotBeNull();
    }

    [Fact]
    public void AddTrackingEvent_ToPickedUpStatus_ShouldSetPickedUpAt()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        var beforeAdd = DateTimeOffset.UtcNow;
        var trackingEvent = ShippingTrackingEvent.Create(
            order.Id, "PICKED_UP", ShippingStatus.PickedUp,
            "Package picked up", "Warehouse", DateTimeOffset.UtcNow);

        // Act
        order.AddTrackingEvent(trackingEvent);

        // Assert
        order.PickedUpAt.ShouldNotBeNull();
        order.PickedUpAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeAdd);
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_FromDraft_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = CreateTestShippingOrder();

        // Act
        order.Cancel("No longer needed");

        // Assert
        order.Status.ShouldBe(ShippingStatus.Cancelled);
    }

    [Fact]
    public void Cancel_FromAwaitingPickup_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();

        // Act
        order.Cancel("Customer cancelled order");

        // Assert
        order.Status.ShouldBe(ShippingStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WithReason_ShouldAppendReasonToNotes()
    {
        // Arrange
        var order = CreateTestShippingOrder(notes: null);

        // Act
        order.Cancel("Out of stock");

        // Assert
        order.Notes.ShouldBe("Cancelled: Out of stock");
    }

    [Fact]
    public void Cancel_WithExistingNotes_ShouldAppendReasonToExistingNotes()
    {
        // Arrange
        var order = CreateTestShippingOrder(notes: "Initial notes");

        // Act
        order.Cancel("Customer request");

        // Assert
        order.Notes.ShouldBe("Initial notes\nCancelled: Customer request");
    }

    [Fact]
    public void Cancel_WithNullReason_ShouldAppendNullReasonToNotes()
    {
        // Arrange
        var order = CreateTestShippingOrder(notes: null);

        // Act
        order.Cancel();

        // Assert
        order.Status.ShouldBe(ShippingStatus.Cancelled);
        order.Notes.ShouldBe("Cancelled: ");
    }

    [Fact]
    public void Cancel_ShouldRaiseShippingOrderCancelledEvent()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder(trackingNumber: "TRK-CANCEL");
        order.ClearDomainEvents();
        var reason = "Address incorrect";

        // Act
        order.Cancel(reason);

        // Assert
        var __evt = order.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<ShippingOrderCancelledEvent>();

        __evt.ShippingOrderId.ShouldBe(order.Id);

        __evt.TrackingNumber.ShouldBe("TRK-CANCEL");

        __evt.PreviousStatus.ShouldBe(ShippingStatus.AwaitingPickup);

        __evt.Reason.ShouldBe(reason);
    }

    [Fact]
    public void Cancel_FromDelivered_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        order.UpdateStatus(ShippingStatus.Delivered);

        // Act
        var act = () => order.Cancel("Too late");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot cancel shipping order in status Delivered");
    }

    [Fact]
    public void Cancel_FromAlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = CreateTestShippingOrder();
        order.Cancel("First cancel");

        // Act
        var act = () => order.Cancel("Second cancel");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot cancel shipping order in status Cancelled");
    }

    [Theory]
    [InlineData(ShippingStatus.Draft)]
    [InlineData(ShippingStatus.AwaitingPickup)]
    [InlineData(ShippingStatus.PickedUp)]
    [InlineData(ShippingStatus.InTransit)]
    [InlineData(ShippingStatus.OutForDelivery)]
    [InlineData(ShippingStatus.DeliveryFailed)]
    [InlineData(ShippingStatus.Returning)]
    [InlineData(ShippingStatus.Returned)]
    public void Cancel_FromCancellableStatuses_ShouldSucceed(ShippingStatus startStatus)
    {
        // Arrange
        var order = CreateTestShippingOrder();
        if (startStatus != ShippingStatus.Draft)
        {
            // Submit the order first to get to AwaitingPickup
            order.SetProviderResponse("TRK-001", null, null, null, 20_000m, 0m, 0m, null, null);
            if (startStatus != ShippingStatus.AwaitingPickup)
            {
                order.UpdateStatus(startStatus);
            }
        }

        // Act
        var act = () => order.Cancel("Reason");

        // Assert
        act.ShouldNotThrow();
        order.Status.ShouldBe(ShippingStatus.Cancelled);
    }

    #endregion

    #region SetEstimatedDeliveryDate

    [Fact]
    public void SetEstimatedDeliveryDate_WithDate_ShouldSetDate()
    {
        // Arrange
        var order = CreateTestShippingOrder();
        var estimatedDate = DateTimeOffset.UtcNow.AddDays(5);

        // Act
        order.SetEstimatedDeliveryDate(estimatedDate);

        // Assert
        order.EstimatedDeliveryDate.ShouldBe(estimatedDate);
    }

    [Fact]
    public void SetEstimatedDeliveryDate_WithNull_ShouldClearDate()
    {
        // Arrange
        var order = CreateSubmittedShippingOrder();
        order.EstimatedDeliveryDate.ShouldNotBeNull();

        // Act
        order.SetEstimatedDeliveryDate(null);

        // Assert
        order.EstimatedDeliveryDate.ShouldBeNull();
    }

    [Fact]
    public void SetEstimatedDeliveryDate_ShouldOverwritePreviousDate()
    {
        // Arrange
        var order = CreateTestShippingOrder();
        var firstDate = DateTimeOffset.UtcNow.AddDays(3);
        var secondDate = DateTimeOffset.UtcNow.AddDays(5);
        order.SetEstimatedDeliveryDate(firstDate);

        // Act
        order.SetEstimatedDeliveryDate(secondDate);

        // Assert
        order.EstimatedDeliveryDate.ShouldBe(secondDate);
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_DraftToDelivered_ShouldTransitionCorrectly()
    {
        // Arrange
        var order = CreateTestShippingOrder();
        order.Status.ShouldBe(ShippingStatus.Draft);

        // Act - Submit to provider
        order.SetProviderResponse("TRK-FULL", "PROV-001", null, null,
            25_000m, 3_000m, 1_000m, DateTimeOffset.UtcNow.AddDays(3), null);
        order.Status.ShouldBe(ShippingStatus.AwaitingPickup);
        order.TotalShippingFee.ShouldBe(29_000m);

        // Act - Picked up
        order.UpdateStatus(ShippingStatus.PickedUp, "Sender warehouse");
        order.Status.ShouldBe(ShippingStatus.PickedUp);
        order.PickedUpAt.ShouldNotBeNull();

        // Act - In transit
        order.UpdateStatus(ShippingStatus.InTransit, "Distribution center");
        order.Status.ShouldBe(ShippingStatus.InTransit);

        // Act - Out for delivery
        order.UpdateStatus(ShippingStatus.OutForDelivery, "Local hub");
        order.Status.ShouldBe(ShippingStatus.OutForDelivery);

        // Act - Delivered
        order.UpdateStatus(ShippingStatus.Delivered, "Customer address");
        order.Status.ShouldBe(ShippingStatus.Delivered);
        order.ActualDeliveryDate.ShouldNotBeNull();
    }

    [Fact]
    public void DomainEvents_ShouldAccumulateAcrossMultipleOperations()
    {
        // Arrange
        var order = CreateTestShippingOrder();
        // ShippingOrderCreatedEvent is already raised

        // Act
        order.SetProviderResponse("TRK-001", null, null, null, 20_000m, 0m, 0m, null, null);
        // ShippingOrderSubmittedEvent raised

        order.UpdateStatus(ShippingStatus.PickedUp, "Warehouse");
        // ShippingOrderStatusChangedEvent raised

        // Assert - Created(1) + Submitted(1) + StatusChanged(1) = 3
        order.DomainEvents.Count().ShouldBe(3);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var order = CreateTestShippingOrder();
        order.DomainEvents.Count().ShouldBeGreaterThan(0);

        // Act
        order.ClearDomainEvents();

        // Assert
        order.DomainEvents.ShouldBeEmpty();
    }

    #endregion
}
