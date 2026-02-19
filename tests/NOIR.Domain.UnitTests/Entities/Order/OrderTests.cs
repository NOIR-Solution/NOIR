using NOIR.Domain.Entities.Order;
using NOIR.Domain.Events.Order;
using NOIR.Domain.ValueObjects;

namespace NOIR.Domain.UnitTests.Entities.Order;

/// <summary>
/// Unit tests for the Order aggregate root entity.
/// Tests factory methods, state transitions, business methods, invariant enforcement,
/// cancellation, returns, refunds, internal notes, and financial recalculation.
/// </summary>
public class OrderTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestOrderNumber = "ORD-20260219-0001";
    private const string TestCustomerEmail = "customer@example.com";
    private const decimal TestSubTotal = 500_000m;
    private const decimal TestGrandTotal = 500_000m;
    private const string TestCurrency = "VND";

    /// <summary>
    /// Helper to create a default valid order for tests.
    /// </summary>
    private static NOIR.Domain.Entities.Order.Order CreateTestOrder(
        string? orderNumber = null,
        string? customerEmail = null,
        decimal subTotal = TestSubTotal,
        decimal grandTotal = TestGrandTotal,
        string currency = TestCurrency,
        string? tenantId = TestTenantId)
    {
        return NOIR.Domain.Entities.Order.Order.Create(
            orderNumber ?? TestOrderNumber,
            customerEmail ?? TestCustomerEmail,
            subTotal,
            grandTotal,
            currency,
            tenantId);
    }

    /// <summary>
    /// Helper to create a test Address value object.
    /// </summary>
    private static Address CreateTestAddress(string fullName = "Nguyen Van A", string phone = "0901234567")
    {
        return new Address
        {
            FullName = fullName,
            Phone = phone,
            AddressLine1 = "123 Le Loi",
            Ward = "Ben Thanh",
            District = "District 1",
            Province = "Ho Chi Minh City",
            Country = "Vietnam"
        };
    }

    /// <summary>
    /// Helper to advance an order through the full happy-path lifecycle to a given status.
    /// </summary>
    private static NOIR.Domain.Entities.Order.Order CreateOrderInStatus(OrderStatus targetStatus)
    {
        var order = CreateTestOrder();
        if (targetStatus == OrderStatus.Pending) return order;

        order.Confirm();
        if (targetStatus == OrderStatus.Confirmed) return order;

        order.StartProcessing();
        if (targetStatus == OrderStatus.Processing) return order;

        order.Ship("TRK-001", "GHTK");
        if (targetStatus == OrderStatus.Shipped) return order;

        order.MarkAsDelivered();
        if (targetStatus == OrderStatus.Delivered) return order;

        order.Complete();
        if (targetStatus == OrderStatus.Completed) return order;

        throw new ArgumentException($"Cannot create order in status {targetStatus} via happy path");
    }

    #region Creation

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidOrder()
    {
        // Act
        var order = NOIR.Domain.Entities.Order.Order.Create(
            TestOrderNumber, TestCustomerEmail, TestSubTotal, TestGrandTotal, TestCurrency, TestTenantId);

        // Assert
        order.Should().NotBeNull();
        order.Id.Should().NotBe(Guid.Empty);
        order.OrderNumber.Should().Be(TestOrderNumber);
        order.CustomerEmail.Should().Be(TestCustomerEmail);
        order.SubTotal.Should().Be(TestSubTotal);
        order.GrandTotal.Should().Be(TestGrandTotal);
        order.Currency.Should().Be(TestCurrency);
        order.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var order = CreateTestOrder();

        // Assert
        order.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public void Create_ShouldInitializeFinancialDefaults()
    {
        // Act
        var order = CreateTestOrder();

        // Assert
        order.DiscountAmount.Should().Be(0);
        order.ShippingAmount.Should().Be(0);
        order.TaxAmount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var order = CreateTestOrder();

        // Assert
        order.CustomerId.Should().BeNull();
        order.ShippingAddress.Should().BeNull();
        order.BillingAddress.Should().BeNull();
        order.ShippingMethod.Should().BeNull();
        order.TrackingNumber.Should().BeNull();
        order.ShippingCarrier.Should().BeNull();
        order.EstimatedDeliveryAt.Should().BeNull();
        order.CustomerPhone.Should().BeNull();
        order.CustomerName.Should().BeNull();
        order.CustomerNotes.Should().BeNull();
        order.InternalNotes.Should().BeNull();
        order.CancellationReason.Should().BeNull();
        order.CancelledAt.Should().BeNull();
        order.ReturnReason.Should().BeNull();
        order.ReturnedAt.Should().BeNull();
        order.ConfirmedAt.Should().BeNull();
        order.ShippedAt.Should().BeNull();
        order.DeliveredAt.Should().BeNull();
        order.CompletedAt.Should().BeNull();
        order.CouponCode.Should().BeNull();
        order.CheckoutSessionId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyCollections()
    {
        // Act
        var order = CreateTestOrder();

        // Assert
        order.Items.Should().NotBeNull();
        order.Items.Should().BeEmpty();
        order.Notes.Should().NotBeNull();
        order.Notes.Should().BeEmpty();
        order.Payments.Should().NotBeNull();
        order.Payments.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldDefaultCurrencyToVND()
    {
        // Act - use default currency parameter
        var order = NOIR.Domain.Entities.Order.Order.Create(
            TestOrderNumber, TestCustomerEmail, TestSubTotal, TestGrandTotal);

        // Assert
        order.Currency.Should().Be("VND");
    }

    [Fact]
    public void Create_WithCustomCurrency_ShouldSetCurrency()
    {
        // Act
        var order = NOIR.Domain.Entities.Order.Order.Create(
            TestOrderNumber, TestCustomerEmail, TestSubTotal, TestGrandTotal, "USD");

        // Assert
        order.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_ShouldRaiseOrderCreatedEvent()
    {
        // Act
        var order = CreateTestOrder();

        // Assert
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OrderId = order.Id,
                OrderNumber = TestOrderNumber,
                CustomerEmail = TestCustomerEmail,
                GrandTotal = TestGrandTotal,
                Currency = TestCurrency
            });
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var order = NOIR.Domain.Entities.Order.Order.Create(
            TestOrderNumber, TestCustomerEmail, TestSubTotal, TestGrandTotal, tenantId: null);

        // Assert
        order.TenantId.Should().BeNull();
    }

    #endregion

    #region State Transitions - Happy Path

    [Fact]
    public void Confirm_FromPending_ShouldTransitionToConfirmed()
    {
        // Arrange
        var order = CreateTestOrder();
        var beforeConfirm = DateTimeOffset.UtcNow;

        // Act
        order.Confirm();

        // Assert
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAt.Should().NotBeNull();
        order.ConfirmedAt.Should().BeOnOrAfter(beforeConfirm);
    }

    [Fact]
    public void Confirm_ShouldRaiseStatusChangedAndConfirmedEvents()
    {
        // Arrange
        var order = CreateTestOrder();
        order.ClearDomainEvents(); // Clear OrderCreatedEvent

        // Act
        order.Confirm();

        // Assert
        order.DomainEvents.Should().HaveCount(2);
        order.DomainEvents.Should().ContainSingle(e => e is OrderStatusChangedEvent)
            .Which.Should().BeOfType<OrderStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OrderId = order.Id,
                OldStatus = OrderStatus.Pending,
                NewStatus = OrderStatus.Confirmed
            });
        order.DomainEvents.Should().ContainSingle(e => e is OrderConfirmedEvent);
    }

    [Fact]
    public void StartProcessing_FromConfirmed_ShouldTransitionToProcessing()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Confirmed);

        // Act
        order.StartProcessing();

        // Assert
        order.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public void StartProcessing_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Confirmed);
        order.ClearDomainEvents();

        // Act
        order.StartProcessing();

        // Assert
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = OrderStatus.Confirmed,
                NewStatus = OrderStatus.Processing
            });
    }

    [Fact]
    public void Ship_FromProcessing_ShouldTransitionToShipped()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Processing);
        var beforeShip = DateTimeOffset.UtcNow;

        // Act
        order.Ship("TRK-12345", "Giao Hang Tiet Kiem");

        // Assert
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().NotBeNull();
        order.ShippedAt.Should().BeOnOrAfter(beforeShip);
        order.TrackingNumber.Should().Be("TRK-12345");
        order.ShippingCarrier.Should().Be("Giao Hang Tiet Kiem");
    }

    [Fact]
    public void Ship_ShouldRaiseStatusChangedAndShippedEvents()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Processing);
        order.ClearDomainEvents();

        // Act
        order.Ship("TRK-001", "GHTK");

        // Assert
        order.DomainEvents.Should().HaveCount(2);
        order.DomainEvents.Should().ContainSingle(e => e is OrderStatusChangedEvent);
        order.DomainEvents.Should().ContainSingle(e => e is OrderShippedEvent)
            .Which.Should().BeOfType<OrderShippedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                TrackingNumber = "TRK-001",
                ShippingCarrier = "GHTK"
            });
    }

    [Fact]
    public void MarkAsDelivered_FromShipped_ShouldTransitionToDelivered()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Shipped);
        var beforeDelivery = DateTimeOffset.UtcNow;

        // Act
        order.MarkAsDelivered();

        // Assert
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
        order.DeliveredAt.Should().BeOnOrAfter(beforeDelivery);
    }

    [Fact]
    public void MarkAsDelivered_ShouldRaiseStatusChangedAndDeliveredEvents()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Shipped);
        order.ClearDomainEvents();

        // Act
        order.MarkAsDelivered();

        // Assert
        order.DomainEvents.Should().HaveCount(2);
        order.DomainEvents.Should().ContainSingle(e => e is OrderStatusChangedEvent);
        order.DomainEvents.Should().ContainSingle(e => e is OrderDeliveredEvent);
    }

    [Fact]
    public void Complete_FromDelivered_ShouldTransitionToCompleted()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Delivered);
        var beforeComplete = DateTimeOffset.UtcNow;

        // Act
        order.Complete();

        // Assert
        order.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().NotBeNull();
        order.CompletedAt.Should().BeOnOrAfter(beforeComplete);
    }

    [Fact]
    public void Complete_ShouldRaiseStatusChangedAndCompletedEvents()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Delivered);
        order.ClearDomainEvents();

        // Act
        order.Complete();

        // Assert
        order.DomainEvents.Should().HaveCount(2);
        order.DomainEvents.Should().ContainSingle(e => e is OrderStatusChangedEvent);
        order.DomainEvents.Should().ContainSingle(e => e is OrderCompletedEvent);
    }

    [Fact]
    public void FullLifecycle_PendingToCompleted_ShouldTransitionThroughAllStatuses()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act & Assert - walk through full happy path
        order.Status.Should().Be(OrderStatus.Pending);

        order.Confirm();
        order.Status.Should().Be(OrderStatus.Confirmed);
        order.ConfirmedAt.Should().NotBeNull();

        order.StartProcessing();
        order.Status.Should().Be(OrderStatus.Processing);

        order.Ship("TRK-FULL", "VNPost");
        order.Status.Should().Be(OrderStatus.Shipped);
        order.ShippedAt.Should().NotBeNull();

        order.MarkAsDelivered();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();

        order.Complete();
        order.Status.Should().Be(OrderStatus.Completed);
        order.CompletedAt.Should().NotBeNull();
    }

    #endregion

    #region State Transitions - Invalid (Confirm)

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Completed)]
    public void Confirm_FromNonPendingStatus_ShouldThrow(OrderStatus startStatus)
    {
        // Arrange
        var order = CreateOrderInStatus(startStatus);

        // Act
        var act = () => order.Confirm();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot confirm order in {startStatus} status");
    }

    [Fact]
    public void Confirm_FromCancelled_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel("Test reason");

        // Act
        var act = () => order.Confirm();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot confirm order in Cancelled status");
    }

    #endregion

    #region State Transitions - Invalid (StartProcessing)

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Completed)]
    public void StartProcessing_FromNonConfirmedStatus_ShouldThrow(OrderStatus startStatus)
    {
        // Arrange
        var order = CreateOrderInStatus(startStatus);

        // Act
        var act = () => order.StartProcessing();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot start processing order in {startStatus} status");
    }

    [Fact]
    public void StartProcessing_FromCancelled_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();

        // Act
        var act = () => order.StartProcessing();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot start processing order in Cancelled status");
    }

    #endregion

    #region State Transitions - Invalid (Ship)

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Completed)]
    public void Ship_FromNonProcessingStatus_ShouldThrow(OrderStatus startStatus)
    {
        // Arrange
        var order = CreateOrderInStatus(startStatus);

        // Act
        var act = () => order.Ship("TRK-001", "GHTK");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot ship order in {startStatus} status");
    }

    [Fact]
    public void Ship_FromCancelled_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();

        // Act
        var act = () => order.Ship("TRK-001", "GHTK");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot ship order in Cancelled status");
    }

    #endregion

    #region State Transitions - Invalid (MarkAsDelivered)

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Completed)]
    public void MarkAsDelivered_FromNonShippedStatus_ShouldThrow(OrderStatus startStatus)
    {
        // Arrange
        var order = CreateOrderInStatus(startStatus);

        // Act
        var act = () => order.MarkAsDelivered();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot mark as delivered order in {startStatus} status");
    }

    [Fact]
    public void MarkAsDelivered_FromCancelled_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();

        // Act
        var act = () => order.MarkAsDelivered();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot mark as delivered order in Cancelled status");
    }

    #endregion

    #region State Transitions - Invalid (Complete)

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed)]
    public void Complete_FromNonDeliveredStatus_ShouldThrow(OrderStatus startStatus)
    {
        // Arrange
        var order = CreateOrderInStatus(startStatus);

        // Act
        var act = () => order.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot complete order in {startStatus} status");
    }

    [Fact]
    public void Complete_FromCancelled_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();

        // Act
        var act = () => order.Complete();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot complete order in Cancelled status");
    }

    #endregion

    #region Cancel

    [Fact]
    public void Cancel_FromPending_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = CreateTestOrder();
        var beforeCancel = DateTimeOffset.UtcNow;

        // Act
        order.Cancel("Customer changed mind");

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Customer changed mind");
        order.CancelledAt.Should().NotBeNull();
        order.CancelledAt.Should().BeOnOrAfter(beforeCancel);
    }

    [Fact]
    public void Cancel_FromConfirmed_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Confirmed);

        // Act
        order.Cancel("Out of stock");

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Out of stock");
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_FromProcessing_ShouldTransitionToCancelled()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Processing);

        // Act
        order.Cancel("Duplicate order");

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().Be("Duplicate order");
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_WithoutReason_ShouldAllowNullReason()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancellationReason.Should().BeNull();
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ShouldRaiseStatusChangedAndCancelledEvents()
    {
        // Arrange
        var order = CreateTestOrder();
        order.ClearDomainEvents();
        var reason = "No longer needed";

        // Act
        order.Cancel(reason);

        // Assert
        order.DomainEvents.Should().HaveCount(2);
        order.DomainEvents.Should().ContainSingle(e => e is OrderStatusChangedEvent)
            .Which.Should().BeOfType<OrderStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = OrderStatus.Pending,
                NewStatus = OrderStatus.Cancelled,
                Reason = reason
            });
        order.DomainEvents.Should().ContainSingle(e => e is OrderCancelledEvent)
            .Which.Should().BeOfType<OrderCancelledEvent>()
            .Which.CancellationReason.Should().Be(reason);
    }

    [Theory]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Completed)]
    public void Cancel_FromNonCancellableStatus_ShouldThrow(OrderStatus startStatus)
    {
        // Arrange
        var order = CreateOrderInStatus(startStatus);

        // Act
        var act = () => order.Cancel("Too late");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot cancel order in {startStatus} status");
    }

    [Fact]
    public void Cancel_FromAlreadyCancelled_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel("First cancel");

        // Act
        var act = () => order.Cancel("Second cancel");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot cancel order in Cancelled status");
    }

    [Fact]
    public void Cancel_FromRefunded_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Confirmed);
        order.MarkAsRefunded(100_000m);

        // Act
        var act = () => order.Cancel("Try cancel refunded");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot cancel order in Refunded status");
    }

    #endregion

    #region Return

    [Fact]
    public void Return_FromDelivered_ShouldTransitionToReturned()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Delivered);
        var beforeReturn = DateTimeOffset.UtcNow;

        // Act
        order.Return("Product damaged");

        // Assert
        order.Status.Should().Be(OrderStatus.Returned);
        order.ReturnReason.Should().Be("Product damaged");
        order.ReturnedAt.Should().NotBeNull();
        order.ReturnedAt.Should().BeOnOrAfter(beforeReturn);
    }

    [Fact]
    public void Return_FromCompleted_ShouldTransitionToReturned()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Completed);

        // Act
        order.Return("Wrong size");

        // Assert
        order.Status.Should().Be(OrderStatus.Returned);
        order.ReturnReason.Should().Be("Wrong size");
        order.ReturnedAt.Should().NotBeNull();
    }

    [Fact]
    public void Return_WithoutReason_ShouldAllowNullReason()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Delivered);

        // Act
        order.Return();

        // Assert
        order.Status.Should().Be(OrderStatus.Returned);
        order.ReturnReason.Should().BeNull();
        order.ReturnedAt.Should().NotBeNull();
    }

    [Fact]
    public void Return_ShouldRaiseStatusChangedAndReturnedEvents()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Delivered);
        order.ClearDomainEvents();
        var reason = "Defective product";

        // Act
        order.Return(reason);

        // Assert
        order.DomainEvents.Should().HaveCount(2);
        order.DomainEvents.Should().ContainSingle(e => e is OrderStatusChangedEvent)
            .Which.Should().BeOfType<OrderStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = OrderStatus.Delivered,
                NewStatus = OrderStatus.Returned,
                Reason = reason
            });
        order.DomainEvents.Should().ContainSingle(e => e is OrderReturnedEvent)
            .Which.Should().BeOfType<OrderReturnedEvent>()
            .Which.ReturnReason.Should().Be(reason);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Processing)]
    [InlineData(OrderStatus.Shipped)]
    public void Return_FromNonReturnableStatus_ShouldThrow(OrderStatus startStatus)
    {
        // Arrange
        var order = CreateOrderInStatus(startStatus);

        // Act
        var act = () => order.Return("Trying to return");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot return order in {startStatus} status");
    }

    [Fact]
    public void Return_FromCancelled_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();

        // Act
        var act = () => order.Return("Trying to return cancelled");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot return order in Cancelled status");
    }

    [Fact]
    public void Return_FromAlreadyReturned_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Delivered);
        order.Return("First return");

        // Act
        var act = () => order.Return("Second return");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot return order in Returned status");
    }

    #endregion

    #region Refund

    [Fact]
    public void MarkAsRefunded_FromConfirmed_ShouldTransitionToRefunded()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Confirmed);
        order.ClearDomainEvents();

        // Act
        order.MarkAsRefunded(250_000m);

        // Assert
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public void MarkAsRefunded_FromProcessing_ShouldSucceed()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Processing);

        // Act
        order.MarkAsRefunded(100_000m);

        // Assert
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public void MarkAsRefunded_FromShipped_ShouldSucceed()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Shipped);

        // Act
        order.MarkAsRefunded(500_000m);

        // Assert
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public void MarkAsRefunded_FromDelivered_ShouldSucceed()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Delivered);

        // Act
        order.MarkAsRefunded(500_000m);

        // Assert
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public void MarkAsRefunded_FromCompleted_ShouldSucceed()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Completed);

        // Act
        order.MarkAsRefunded(500_000m);

        // Assert
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public void MarkAsRefunded_FromReturned_ShouldSucceed()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Delivered);
        order.Return("Defective");

        // Act
        order.MarkAsRefunded(500_000m);

        // Assert
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    [Fact]
    public void MarkAsRefunded_ShouldRaiseStatusChangedAndRefundedEvents()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Confirmed);
        order.ClearDomainEvents();

        // Act
        order.MarkAsRefunded(250_000m);

        // Assert
        order.DomainEvents.Should().HaveCount(2);
        order.DomainEvents.Should().ContainSingle(e => e is OrderStatusChangedEvent)
            .Which.Should().BeOfType<OrderStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = OrderStatus.Confirmed,
                NewStatus = OrderStatus.Refunded
            });
        order.DomainEvents.Should().ContainSingle(e => e is OrderRefundedEvent)
            .Which.Should().BeOfType<OrderRefundedEvent>()
            .Which.RefundAmount.Should().Be(250_000m);
    }

    [Fact]
    public void MarkAsRefunded_FromPending_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        var act = () => order.MarkAsRefunded(100_000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot refund order in Pending status");
    }

    [Fact]
    public void MarkAsRefunded_FromCancelled_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Cancel();

        // Act
        var act = () => order.MarkAsRefunded(100_000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot refund order in Cancelled status");
    }

    [Fact]
    public void MarkAsRefunded_FromAlreadyRefunded_ShouldThrow()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Confirmed);
        order.MarkAsRefunded(100_000m);

        // Act
        var act = () => order.MarkAsRefunded(50_000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot refund order in Refunded status");
    }

    #endregion

    #region Business Methods - SetCustomerInfo

    [Fact]
    public void SetCustomerInfo_WithAllParameters_ShouldSetAllFields()
    {
        // Arrange
        var order = CreateTestOrder();
        var customerId = Guid.NewGuid();

        // Act
        order.SetCustomerInfo(customerId, "Nguyen Van A", "0901234567");

        // Assert
        order.CustomerId.Should().Be(customerId);
        order.CustomerName.Should().Be("Nguyen Van A");
        order.CustomerPhone.Should().Be("0901234567");
    }

    [Fact]
    public void SetCustomerInfo_WithNullCustomerId_ShouldSetGuestOrder()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.SetCustomerInfo(null, "Guest User", "0909876543");

        // Assert
        order.CustomerId.Should().BeNull();
        order.CustomerName.Should().Be("Guest User");
        order.CustomerPhone.Should().Be("0909876543");
    }

    [Fact]
    public void SetCustomerInfo_WithNullNameAndPhone_ShouldAllowNulls()
    {
        // Arrange
        var order = CreateTestOrder();
        var customerId = Guid.NewGuid();

        // Act
        order.SetCustomerInfo(customerId, null, null);

        // Assert
        order.CustomerId.Should().Be(customerId);
        order.CustomerName.Should().BeNull();
        order.CustomerPhone.Should().BeNull();
    }

    [Fact]
    public void SetCustomerInfo_ShouldOverwritePreviousValues()
    {
        // Arrange
        var order = CreateTestOrder();
        var firstCustomerId = Guid.NewGuid();
        var secondCustomerId = Guid.NewGuid();
        order.SetCustomerInfo(firstCustomerId, "First", "111");

        // Act
        order.SetCustomerInfo(secondCustomerId, "Second", "222");

        // Assert
        order.CustomerId.Should().Be(secondCustomerId);
        order.CustomerName.Should().Be("Second");
        order.CustomerPhone.Should().Be("222");
    }

    #endregion

    #region Business Methods - Addresses

    [Fact]
    public void SetShippingAddress_ShouldSetAddress()
    {
        // Arrange
        var order = CreateTestOrder();
        var address = CreateTestAddress();

        // Act
        order.SetShippingAddress(address);

        // Assert
        order.ShippingAddress.Should().NotBeNull();
        order.ShippingAddress.Should().Be(address);
        order.ShippingAddress!.FullName.Should().Be("Nguyen Van A");
        order.ShippingAddress.AddressLine1.Should().Be("123 Le Loi");
        order.ShippingAddress.District.Should().Be("District 1");
        order.ShippingAddress.Province.Should().Be("Ho Chi Minh City");
    }

    [Fact]
    public void SetBillingAddress_ShouldSetAddress()
    {
        // Arrange
        var order = CreateTestOrder();
        var address = CreateTestAddress("Tran Thi B", "0907654321");

        // Act
        order.SetBillingAddress(address);

        // Assert
        order.BillingAddress.Should().NotBeNull();
        order.BillingAddress.Should().Be(address);
        order.BillingAddress!.FullName.Should().Be("Tran Thi B");
    }

    [Fact]
    public void SetShippingAndBillingAddress_CanBeDifferent()
    {
        // Arrange
        var order = CreateTestOrder();
        var shippingAddress = CreateTestAddress("Ship To", "0901111111");
        var billingAddress = CreateTestAddress("Bill To", "0902222222");

        // Act
        order.SetShippingAddress(shippingAddress);
        order.SetBillingAddress(billingAddress);

        // Assert
        order.ShippingAddress.Should().NotBe(order.BillingAddress);
        order.ShippingAddress!.FullName.Should().Be("Ship To");
        order.BillingAddress!.FullName.Should().Be("Bill To");
    }

    [Fact]
    public void SetShippingAddress_ShouldOverwritePreviousAddress()
    {
        // Arrange
        var order = CreateTestOrder();
        var firstAddress = CreateTestAddress("First");
        var secondAddress = CreateTestAddress("Second");
        order.SetShippingAddress(firstAddress);

        // Act
        order.SetShippingAddress(secondAddress);

        // Assert
        order.ShippingAddress!.FullName.Should().Be("Second");
    }

    #endregion

    #region Business Methods - SetShippingDetails

    [Fact]
    public void SetShippingDetails_ShouldSetMethodAndAmount()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.SetShippingDetails("Express Delivery", 30_000m);

        // Assert
        order.ShippingMethod.Should().Be("Express Delivery");
        order.ShippingAmount.Should().Be(30_000m);
        order.EstimatedDeliveryAt.Should().BeNull();
    }

    [Fact]
    public void SetShippingDetails_WithEstimatedDelivery_ShouldSetDate()
    {
        // Arrange
        var order = CreateTestOrder();
        var estimatedDelivery = DateTimeOffset.UtcNow.AddDays(3);

        // Act
        order.SetShippingDetails("Standard Shipping", 15_000m, estimatedDelivery);

        // Assert
        order.ShippingMethod.Should().Be("Standard Shipping");
        order.ShippingAmount.Should().Be(15_000m);
        order.EstimatedDeliveryAt.Should().Be(estimatedDelivery);
    }

    [Fact]
    public void SetShippingDetails_ShouldRecalculateGrandTotal()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 500_000m, grandTotal: 500_000m);

        // Act
        order.SetShippingDetails("Express", 30_000m);

        // Assert - GrandTotal = SubTotal - Discount + Shipping + Tax = 500000 - 0 + 30000 + 0
        order.GrandTotal.Should().Be(530_000m);
    }

    #endregion

    #region Business Methods - SetDiscount

    [Fact]
    public void SetDiscount_ShouldSetDiscountAmountAndCouponCode()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.SetDiscount(50_000m, "SUMMER2026");

        // Assert
        order.DiscountAmount.Should().Be(50_000m);
        order.CouponCode.Should().Be("SUMMER2026");
    }

    [Fact]
    public void SetDiscount_WithoutCouponCode_ShouldAllowNull()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.SetDiscount(25_000m);

        // Assert
        order.DiscountAmount.Should().Be(25_000m);
        order.CouponCode.Should().BeNull();
    }

    [Fact]
    public void SetDiscount_ShouldRecalculateGrandTotal()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 500_000m, grandTotal: 500_000m);

        // Act
        order.SetDiscount(100_000m, "DISCOUNT100");

        // Assert - GrandTotal = SubTotal - Discount + Shipping + Tax = 500000 - 100000 + 0 + 0
        order.GrandTotal.Should().Be(400_000m);
    }

    [Fact]
    public void SetDiscount_CombinedWithShipping_ShouldRecalculateCorrectly()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 500_000m, grandTotal: 500_000m);
        order.SetShippingDetails("Express", 30_000m);

        // Act
        order.SetDiscount(50_000m);

        // Assert - GrandTotal = 500000 - 50000 + 30000 + 0 = 480000
        order.GrandTotal.Should().Be(480_000m);
    }

    #endregion

    #region Business Methods - SetTax

    [Fact]
    public void SetTax_ShouldSetTaxAmount()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.SetTax(50_000m);

        // Assert
        order.TaxAmount.Should().Be(50_000m);
    }

    [Fact]
    public void SetTax_ShouldRecalculateGrandTotal()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 500_000m, grandTotal: 500_000m);

        // Act
        order.SetTax(50_000m);

        // Assert - GrandTotal = SubTotal - Discount + Shipping + Tax = 500000 - 0 + 0 + 50000
        order.GrandTotal.Should().Be(550_000m);
    }

    [Fact]
    public void SetTax_CombinedWithDiscountAndShipping_ShouldRecalculateCorrectly()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 1_000_000m, grandTotal: 1_000_000m);
        order.SetShippingDetails("Express", 30_000m);
        order.SetDiscount(100_000m, "PROMO");

        // Act
        order.SetTax(80_000m);

        // Assert - GrandTotal = 1000000 - 100000 + 30000 + 80000 = 1010000
        order.GrandTotal.Should().Be(1_010_000m);
    }

    #endregion

    #region Business Methods - SetCustomerNotes

    [Fact]
    public void SetCustomerNotes_ShouldSetNotes()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.SetCustomerNotes("Please deliver before 5 PM");

        // Assert
        order.CustomerNotes.Should().Be("Please deliver before 5 PM");
    }

    [Fact]
    public void SetCustomerNotes_WithNull_ShouldClearNotes()
    {
        // Arrange
        var order = CreateTestOrder();
        order.SetCustomerNotes("Initial notes");

        // Act
        order.SetCustomerNotes(null);

        // Assert
        order.CustomerNotes.Should().BeNull();
    }

    [Fact]
    public void SetCustomerNotes_ShouldOverwritePreviousNotes()
    {
        // Arrange
        var order = CreateTestOrder();
        order.SetCustomerNotes("First note");

        // Act
        order.SetCustomerNotes("Second note");

        // Assert
        order.CustomerNotes.Should().Be("Second note");
    }

    #endregion

    #region Business Methods - SetCheckoutSessionId

    [Fact]
    public void SetCheckoutSessionId_ShouldSetSessionId()
    {
        // Arrange
        var order = CreateTestOrder();
        var sessionId = Guid.NewGuid();

        // Act
        order.SetCheckoutSessionId(sessionId);

        // Assert
        order.CheckoutSessionId.Should().Be(sessionId);
    }

    [Fact]
    public void SetCheckoutSessionId_ShouldOverwritePreviousValue()
    {
        // Arrange
        var order = CreateTestOrder();
        var firstSessionId = Guid.NewGuid();
        var secondSessionId = Guid.NewGuid();
        order.SetCheckoutSessionId(firstSessionId);

        // Act
        order.SetCheckoutSessionId(secondSessionId);

        // Assert
        order.CheckoutSessionId.Should().Be(secondSessionId);
    }

    #endregion

    #region Business Methods - AddInternalNote

    [Fact]
    public void AddInternalNote_FirstNote_ShouldSetInternalNotes()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.AddInternalNote("Customer called about delivery");

        // Assert
        order.InternalNotes.Should().Be("Customer called about delivery");
    }

    [Fact]
    public void AddInternalNote_SecondNote_ShouldAppendWithSeparator()
    {
        // Arrange
        var order = CreateTestOrder();
        order.AddInternalNote("First note");

        // Act
        order.AddInternalNote("Second note");

        // Assert
        order.InternalNotes.Should().Be("First note\n---\nSecond note");
    }

    [Fact]
    public void AddInternalNote_MultipleNotes_ShouldChainCorrectly()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act
        order.AddInternalNote("Note 1");
        order.AddInternalNote("Note 2");
        order.AddInternalNote("Note 3");

        // Assert
        order.InternalNotes.Should().Be("Note 1\n---\nNote 2\n---\nNote 3");
    }

    [Fact]
    public void AddInternalNote_ShouldRaiseOrderNoteAddedEvent()
    {
        // Arrange
        var order = CreateTestOrder();
        order.ClearDomainEvents();

        // Act
        order.AddInternalNote("Staff note");

        // Assert
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderNoteAddedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OrderId = order.Id,
                Note = "Staff note",
                IsInternal = true
            });
    }

    #endregion

    #region AddItem and SubTotal Recalculation

    [Fact]
    public void AddItem_ShouldAddItemToCollection()
    {
        // Arrange
        var order = CreateTestOrder();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        // Act
        var item = order.AddItem(
            productId, variantId,
            "Ao Thun Nam", "Size M - Blue",
            250_000m, 2,
            "SKU-AT-001", "https://img.example.com/ao-thun.jpg",
            "Color: Blue, Size: M");

        // Assert
        order.Items.Should().HaveCount(1);
        item.Should().NotBeNull();
        item.OrderId.Should().Be(order.Id);
        item.ProductId.Should().Be(productId);
        item.ProductVariantId.Should().Be(variantId);
        item.ProductName.Should().Be("Ao Thun Nam");
        item.VariantName.Should().Be("Size M - Blue");
        item.UnitPrice.Should().Be(250_000m);
        item.Quantity.Should().Be(2);
        item.Sku.Should().Be("SKU-AT-001");
        item.ImageUrl.Should().Be("https://img.example.com/ao-thun.jpg");
        item.OptionsSnapshot.Should().Be("Color: Blue, Size: M");
    }

    [Fact]
    public void AddItem_ShouldRecalculateSubTotal()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);

        // Act
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product A", "Variant A", 100_000m, 3);

        // Assert - SubTotal = 100000 * 3 = 300000
        order.SubTotal.Should().Be(300_000m);
    }

    [Fact]
    public void AddItem_ShouldRecalculateGrandTotal()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);

        // Act
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product A", "Variant A", 100_000m, 2);

        // Assert - GrandTotal = SubTotal - Discount + Shipping + Tax = 200000 - 0 + 0 + 0
        order.GrandTotal.Should().Be(200_000m);
    }

    [Fact]
    public void AddItem_MultipleItems_ShouldSumSubTotal()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);

        // Act
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product A", "Variant A", 100_000m, 2); // 200,000
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product B", "Variant B", 150_000m, 3); // 450,000

        // Assert
        order.Items.Should().HaveCount(2);
        order.SubTotal.Should().Be(650_000m);
        order.GrandTotal.Should().Be(650_000m);
    }

    [Fact]
    public void AddItem_WithExistingDiscountAndShipping_ShouldRecalculateGrandTotalCorrectly()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);
        order.SetShippingDetails("Standard", 25_000m);
        order.SetDiscount(10_000m, "WELCOME10");
        order.SetTax(5_000m);

        // Act
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 200_000m, 1);

        // Assert - GrandTotal = 200000 - 10000 + 25000 + 5000 = 220000
        order.SubTotal.Should().Be(200_000m);
        order.GrandTotal.Should().Be(220_000m);
    }

    [Fact]
    public void AddItem_WithMinimalParameters_ShouldDefaultOptionalToNull()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);

        // Act
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 50_000m, 1);

        // Assert
        item.Sku.Should().BeNull();
        item.ImageUrl.Should().BeNull();
        item.OptionsSnapshot.Should().BeNull();
    }

    [Fact]
    public void AddItem_ShouldSetTenantIdFromOrder()
    {
        // Arrange
        var order = CreateTestOrder(tenantId: "tenant-xyz");

        // Act
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 50_000m, 1);

        // Assert
        item.TenantId.Should().Be("tenant-xyz");
    }

    #endregion

    #region OrderItem Calculations

    [Fact]
    public void OrderItem_Subtotal_ShouldBeUnitPriceTimesQuantity()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);

        // Act
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 150_000m, 4);

        // Assert
        item.Subtotal.Should().Be(600_000m);
    }

    [Fact]
    public void OrderItem_LineTotal_ShouldAccountForDiscountAndTax()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 100_000m, 3);

        // Act
        item.SetDiscount(20_000m);
        item.SetTax(15_000m);

        // Assert - LineTotal = (100000 * 3) - 20000 + 15000 = 295000
        item.LineTotal.Should().Be(295_000m);
    }

    [Fact]
    public void OrderItem_LineTotal_WithNoDiscountOrTax_ShouldEqualSubtotal()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);

        // Act
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 200_000m, 2);

        // Assert
        item.LineTotal.Should().Be(item.Subtotal);
        item.LineTotal.Should().Be(400_000m);
    }

    [Fact]
    public void OrderItem_Create_ShouldInitializeDiscountAndTaxToZero()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);

        // Act
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 100_000m, 1);

        // Assert
        item.DiscountAmount.Should().Be(0);
        item.TaxAmount.Should().Be(0);
    }

    [Fact]
    public void OrderItem_SetDiscount_WithNegativeAmount_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 100_000m, 1);

        // Act
        var act = () => item.SetDiscount(-10_000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Discount amount cannot be negative");
    }

    [Fact]
    public void OrderItem_SetTax_WithNegativeAmount_ShouldThrow()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 100_000m, 1);

        // Act
        var act = () => item.SetTax(-5_000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Tax amount cannot be negative");
    }

    [Fact]
    public void OrderItem_SetDiscount_WithZero_ShouldSucceed()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 100_000m, 1);
        item.SetDiscount(50_000m);

        // Act
        item.SetDiscount(0m);

        // Assert
        item.DiscountAmount.Should().Be(0);
    }

    [Fact]
    public void OrderItem_SetTax_WithZero_ShouldSucceed()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 100_000m, 1);
        item.SetTax(10_000m);

        // Act
        item.SetTax(0m);

        // Assert
        item.TaxAmount.Should().Be(0);
    }

    #endregion

    #region OrderNote

    [Fact]
    public void OrderNote_Create_ShouldSetAllProperties()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        var note = OrderNote.Create(orderId, "Urgent: contact customer", "user-123", "Admin User", TestTenantId);

        // Assert
        note.Should().NotBeNull();
        note.Id.Should().NotBe(Guid.Empty);
        note.OrderId.Should().Be(orderId);
        note.Content.Should().Be("Urgent: contact customer");
        note.CreatedByUserId.Should().Be("user-123");
        note.CreatedByUserName.Should().Be("Admin User");
        note.IsInternal.Should().BeTrue();
        note.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void OrderNote_Create_ShouldAlwaysBeInternal()
    {
        // Act
        var note = OrderNote.Create(Guid.NewGuid(), "Note content", "user-1", "User Name");

        // Assert
        note.IsInternal.Should().BeTrue();
    }

    [Fact]
    public void OrderNote_Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var note = OrderNote.Create(Guid.NewGuid(), "Note", "user-1", "User");

        // Assert
        note.TenantId.Should().BeNull();
    }

    #endregion

    #region Financial Recalculation - Grand Total Formula

    [Fact]
    public void GrandTotal_ShouldFollowFormula_SubTotal_Minus_Discount_Plus_Shipping_Plus_Tax()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 1_000_000m, grandTotal: 1_000_000m);

        // Act - set all financial components
        order.SetDiscount(150_000m);
        order.SetShippingDetails("Premium", 50_000m);
        order.SetTax(80_000m);

        // Assert - GrandTotal = 1000000 - 150000 + 50000 + 80000 = 980000
        order.GrandTotal.Should().Be(980_000m);
    }

    [Fact]
    public void GrandTotal_WithZeroComponents_ShouldEqualSubTotal()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 500_000m, grandTotal: 500_000m);

        // Assert - no discount, shipping, or tax applied
        order.GrandTotal.Should().Be(order.SubTotal);
    }

    [Fact]
    public void GrandTotal_WhenItemsAdded_ShouldRecalculateFromItems()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);
        order.SetShippingDetails("Standard", 20_000m);
        order.SetDiscount(10_000m);
        order.SetTax(5_000m);

        // Act - add items that change SubTotal
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P1", "V1", 100_000m, 2); // 200,000
        order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "P2", "V2", 50_000m, 3);  // 150,000

        // Assert - SubTotal = 350000, GrandTotal = 350000 - 10000 + 20000 + 5000 = 365000
        order.SubTotal.Should().Be(350_000m);
        order.GrandTotal.Should().Be(365_000m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Order_TimestampsPreservation_AfterFullLifecycle()
    {
        // Arrange
        var order = CreateTestOrder();

        // Act - go through the entire lifecycle
        order.Confirm();
        var confirmedAt = order.ConfirmedAt;

        order.StartProcessing();

        order.Ship("TRK-001", "GHTK");
        var shippedAt = order.ShippedAt;

        order.MarkAsDelivered();
        var deliveredAt = order.DeliveredAt;

        order.Complete();
        var completedAt = order.CompletedAt;

        // Assert - all timestamps should be preserved
        order.ConfirmedAt.Should().Be(confirmedAt);
        order.ShippedAt.Should().Be(shippedAt);
        order.DeliveredAt.Should().Be(deliveredAt);
        order.CompletedAt.Should().Be(completedAt);
        order.CancelledAt.Should().BeNull();
        order.ReturnedAt.Should().BeNull();
    }

    [Fact]
    public void Order_CancelledTimestamps_ShouldNotAffectOtherTimestamps()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Confirmed);
        var confirmedAt = order.ConfirmedAt;

        // Act
        order.Cancel("Changed mind");

        // Assert - ConfirmedAt should still be set from before cancellation
        order.ConfirmedAt.Should().Be(confirmedAt);
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Order_DomainEvents_ShouldAccumulateAcrossMultipleOperations()
    {
        // Arrange
        var order = CreateTestOrder();
        // OrderCreatedEvent is already raised

        // Act
        order.Confirm();
        order.StartProcessing();

        // Assert - OrderCreated(1) + StatusChanged+Confirmed(2) + StatusChanged(1) = 4
        order.DomainEvents.Should().HaveCount(4);
    }

    [Fact]
    public void Order_ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var order = CreateTestOrder();
        order.Confirm();
        order.DomainEvents.Should().HaveCountGreaterThan(0);

        // Act
        order.ClearDomainEvents();

        // Assert
        order.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Order_AddItem_WithSingleQuantity_ShouldCalculateCorrectly()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);

        // Act
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product", "Variant", 99_000m, 1);

        // Assert
        item.Subtotal.Should().Be(99_000m);
        item.LineTotal.Should().Be(99_000m);
        order.SubTotal.Should().Be(99_000m);
    }

    [Fact]
    public void Order_AddItem_WithLargeQuantity_ShouldCalculateCorrectly()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 0m, grandTotal: 0m);

        // Act
        var item = order.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Bulk Product", "Variant", 10_000m, 1000);

        // Assert
        item.Subtotal.Should().Be(10_000_000m);
        order.SubTotal.Should().Be(10_000_000m);
    }

    [Fact]
    public void Order_MultipleUniqueIds_ShouldBeGenerated()
    {
        // Act
        var order1 = CreateTestOrder(orderNumber: "ORD-001");
        var order2 = CreateTestOrder(orderNumber: "ORD-002");

        // Assert
        order1.Id.Should().NotBe(order2.Id);
    }

    [Fact]
    public void Order_SetDiscount_ThenChangeAgain_ShouldRecalculateCorrectly()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 500_000m, grandTotal: 500_000m);
        order.SetDiscount(100_000m);
        order.GrandTotal.Should().Be(400_000m);

        // Act - change discount to larger amount
        order.SetDiscount(200_000m);

        // Assert
        order.DiscountAmount.Should().Be(200_000m);
        order.GrandTotal.Should().Be(300_000m);
    }

    [Fact]
    public void Order_SetShippingDetails_ThenChangeAgain_ShouldRecalculateCorrectly()
    {
        // Arrange
        var order = CreateTestOrder(subTotal: 500_000m, grandTotal: 500_000m);
        order.SetShippingDetails("Standard", 20_000m);
        order.GrandTotal.Should().Be(520_000m);

        // Act - upgrade shipping
        order.SetShippingDetails("Express", 50_000m);

        // Assert
        order.ShippingMethod.Should().Be("Express");
        order.ShippingAmount.Should().Be(50_000m);
        order.GrandTotal.Should().Be(550_000m);
    }

    [Fact]
    public void Order_ReturnFromCompleted_ShouldPreserveCompletedAt()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Completed);
        var completedAt = order.CompletedAt;

        // Act
        order.Return("Not satisfied");

        // Assert
        order.CompletedAt.Should().Be(completedAt);
        order.ReturnedAt.Should().NotBeNull();
        order.ReturnReason.Should().Be("Not satisfied");
    }

    [Fact]
    public void Order_RefundAfterReturn_ShouldSucceed()
    {
        // Arrange
        var order = CreateOrderInStatus(OrderStatus.Delivered);
        order.Return("Damaged goods");

        // Act
        order.MarkAsRefunded(500_000m);

        // Assert
        order.Status.Should().Be(OrderStatus.Refunded);
    }

    #endregion
}
