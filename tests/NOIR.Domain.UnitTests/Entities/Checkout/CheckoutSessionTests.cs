using NOIR.Domain.Entities.Checkout;
using NOIR.Domain.Events.Checkout;
using NOIR.Domain.ValueObjects;

namespace NOIR.Domain.UnitTests.Entities.Checkout;

/// <summary>
/// Unit tests for the CheckoutSession aggregate root entity.
/// Tests factory methods, state machine transitions, address/shipping/payment selection,
/// coupon handling, grand total calculation, expiry, and domain event raising.
/// </summary>
public class CheckoutSessionTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";
    private const string TestEmail = "test@example.com";
    private const string TestCurrency = "VND";

    private static readonly Guid TestCartId = Guid.NewGuid();

    #region Helper Methods

    private static CheckoutSession CreateStartedSession(
        decimal subTotal = 500000m,
        string? userId = TestUserId,
        string? tenantId = TestTenantId)
    {
        return CheckoutSession.Create(TestCartId, TestEmail, subTotal, TestCurrency, userId, tenantId);
    }

    private static Address CreateTestAddress(string fullName = "Nguyen Van A")
    {
        return new Address
        {
            FullName = fullName,
            Phone = "0901234567",
            AddressLine1 = "123 Le Loi",
            AddressLine2 = null,
            Ward = "Ben Thanh",
            District = "District 1",
            Province = "Ho Chi Minh City",
            Country = "Vietnam",
            PostalCode = "700000"
        };
    }

    private static CheckoutSession CreateSessionWithAddress()
    {
        var session = CreateStartedSession();
        session.SetShippingAddress(CreateTestAddress());
        return session;
    }

    private static CheckoutSession CreateSessionWithShipping()
    {
        var session = CreateSessionWithAddress();
        session.SelectShippingMethod("Express", 50000m, DateTimeOffset.UtcNow.AddDays(3));
        return session;
    }

    private static CheckoutSession CreateSessionWithPayment()
    {
        var session = CreateSessionWithShipping();
        session.SelectPaymentMethod(PaymentMethod.COD, null);
        return session;
    }

    private static CheckoutSession CreateSessionReadyToComplete()
    {
        var session = CreateSessionWithPayment();
        session.MarkAsPaymentProcessing();
        return session;
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateSessionWithCorrectProperties()
    {
        // Arrange & Act
        var session = CheckoutSession.Create(TestCartId, TestEmail, 500000m, TestCurrency, TestUserId, TestTenantId);

        // Assert
        session.Should().NotBeNull();
        session.Id.Should().NotBe(Guid.Empty);
        session.CartId.Should().Be(TestCartId);
        session.UserId.Should().Be(TestUserId);
        session.CustomerEmail.Should().Be(TestEmail);
        session.SubTotal.Should().Be(500000m);
        session.GrandTotal.Should().Be(500000m);
        session.Currency.Should().Be(TestCurrency);
        session.TenantId.Should().Be(TestTenantId);
        session.Status.Should().Be(CheckoutSessionStatus.Started);
    }

    [Fact]
    public void Create_ShouldDefaultToStartedStatus()
    {
        // Act
        var session = CreateStartedSession();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Started);
    }

    [Fact]
    public void Create_ShouldSetExpiresAt15MinutesFromNow()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var session = CreateStartedSession();

        // Assert
        session.ExpiresAt.Should().BeAfter(before.AddMinutes(14));
        session.ExpiresAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddMinutes(15));
    }

    [Fact]
    public void Create_ShouldInitializeDefaultTotals()
    {
        // Act
        var session = CreateStartedSession(subTotal: 300000m);

        // Assert
        session.SubTotal.Should().Be(300000m);
        session.DiscountAmount.Should().Be(0m);
        session.TaxAmount.Should().Be(0m);
        session.ShippingCost.Should().Be(0m);
        session.GrandTotal.Should().Be(300000m);
    }

    [Fact]
    public void Create_ShouldDefaultBillingSameAsShippingToTrue()
    {
        // Act
        var session = CreateStartedSession();

        // Assert
        session.BillingSameAsShipping.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutUserId_ShouldAllowGuestCheckout()
    {
        // Act
        var session = CheckoutSession.Create(TestCartId, TestEmail, 100000m, TestCurrency, userId: null);

        // Assert
        session.UserId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldRaiseCheckoutSessionCreatedEvent()
    {
        // Act
        var session = CreateStartedSession();

        // Assert
        session.DomainEvents.Should().ContainSingle();
        var createdEvent = session.DomainEvents.OfType<CheckoutSessionCreatedEvent>().Single();
        createdEvent.SessionId.Should().Be(session.Id);
        createdEvent.CartId.Should().Be(TestCartId);
        createdEvent.UserId.Should().Be(TestUserId);
    }

    [Fact]
    public void Create_ShouldHaveNoAddresses()
    {
        // Act
        var session = CreateStartedSession();

        // Assert
        session.ShippingAddress.Should().BeNull();
        session.BillingAddress.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldHaveNoShippingOrPayment()
    {
        // Act
        var session = CreateStartedSession();

        // Assert
        session.ShippingMethod.Should().BeNull();
        session.PaymentMethod.Should().BeNull();
        session.PaymentGatewayId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldHaveNoOrderReference()
    {
        // Act
        var session = CreateStartedSession();

        // Assert
        session.OrderId.Should().BeNull();
        session.OrderNumber.Should().BeNull();
    }

    #endregion

    #region SetCustomerInfo Tests

    [Fact]
    public void SetCustomerInfo_ShouldUpdateNameAndPhone()
    {
        // Arrange
        var session = CreateStartedSession();

        // Act
        session.SetCustomerInfo("Nguyen Van B", "0909876543");

        // Assert
        session.CustomerName.Should().Be("Nguyen Van B");
        session.CustomerPhone.Should().Be("0909876543");
    }

    [Fact]
    public void SetCustomerInfo_WithEmail_ShouldUpdateEmail()
    {
        // Arrange
        var session = CreateStartedSession();

        // Act
        session.SetCustomerInfo("Name", "0901234567", "newemail@example.com");

        // Assert
        session.CustomerEmail.Should().Be("newemail@example.com");
    }

    [Fact]
    public void SetCustomerInfo_WithNullEmail_ShouldNotOverwriteExistingEmail()
    {
        // Arrange
        var session = CreateStartedSession();
        var originalEmail = session.CustomerEmail;

        // Act
        session.SetCustomerInfo("Name", "0901234567");

        // Assert
        session.CustomerEmail.Should().Be(originalEmail);
    }

    [Fact]
    public void SetCustomerInfo_WithEmptyEmail_ShouldNotOverwriteExistingEmail()
    {
        // Arrange
        var session = CreateStartedSession();
        var originalEmail = session.CustomerEmail;

        // Act
        session.SetCustomerInfo("Name", "0901234567", string.Empty);

        // Assert
        session.CustomerEmail.Should().Be(originalEmail);
    }

    #endregion

    #region SetShippingAddress Tests

    [Fact]
    public void SetShippingAddress_OnStartedSession_ShouldSetAddressAndTransitionToAddressComplete()
    {
        // Arrange
        var session = CreateStartedSession();
        var address = CreateTestAddress();

        // Act
        session.SetShippingAddress(address);

        // Assert
        session.ShippingAddress.Should().Be(address);
        session.Status.Should().Be(CheckoutSessionStatus.AddressComplete);
    }

    [Fact]
    public void SetShippingAddress_WithBillingSameAsShipping_ShouldCopyToBillingAddress()
    {
        // Arrange
        var session = CreateStartedSession();
        session.BillingSameAsShipping.Should().BeTrue(); // default
        var address = CreateTestAddress();

        // Act
        session.SetShippingAddress(address);

        // Assert
        session.BillingAddress.Should().Be(address);
    }

    [Fact]
    public void SetShippingAddress_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var session = CreateStartedSession();
        session.ClearDomainEvents();
        var address = CreateTestAddress();

        // Act
        session.SetShippingAddress(address);

        // Assert
        var statusEvent = session.DomainEvents.OfType<CheckoutSessionStatusChangedEvent>().Single();
        statusEvent.SessionId.Should().Be(session.Id);
        statusEvent.OldStatus.Should().Be(CheckoutSessionStatus.Started);
        statusEvent.NewStatus.Should().Be(CheckoutSessionStatus.AddressComplete);
    }

    [Fact]
    public void SetShippingAddress_ShouldRaiseCheckoutAddressSetEvent()
    {
        // Arrange
        var session = CreateStartedSession();
        session.ClearDomainEvents();

        // Act
        session.SetShippingAddress(CreateTestAddress());

        // Assert
        var addressEvent = session.DomainEvents.OfType<CheckoutAddressSetEvent>().Single();
        addressEvent.SessionId.Should().Be(session.Id);
        addressEvent.AddressType.Should().Be("Shipping");
    }

    [Fact]
    public void SetShippingAddress_OnAddressCompleteSession_ShouldUpdateAddressWithoutStatusChange()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        session.ClearDomainEvents();
        var newAddress = CreateTestAddress("Tran Van B");

        // Act
        session.SetShippingAddress(newAddress);

        // Assert
        session.ShippingAddress!.FullName.Should().Be("Tran Van B");
        session.Status.Should().Be(CheckoutSessionStatus.AddressComplete);
        // No status change event - already in AddressComplete
        session.DomainEvents.OfType<CheckoutSessionStatusChangedEvent>().Should().BeEmpty();
    }

    [Theory]
    [InlineData(CheckoutSessionStatus.Completed)]
    [InlineData(CheckoutSessionStatus.Expired)]
    [InlineData(CheckoutSessionStatus.Abandoned)]
    public void SetShippingAddress_OnTerminalStatus_ShouldThrow(CheckoutSessionStatus terminalStatus)
    {
        // Arrange
        var session = CreateStartedSession();
        switch (terminalStatus)
        {
            case CheckoutSessionStatus.Completed:
                session.SetShippingAddress(CreateTestAddress());
                session.SelectShippingMethod("Standard", 30000m);
                session.SelectPaymentMethod(PaymentMethod.COD, null);
                session.MarkAsPaymentProcessing();
                session.Complete(Guid.NewGuid(), "ORD-001");
                break;
            case CheckoutSessionStatus.Expired:
                session.MarkAsExpired();
                break;
            case CheckoutSessionStatus.Abandoned:
                session.MarkAsAbandoned();
                break;
        }

        // Act
        var act = () => session.SetShippingAddress(CreateTestAddress());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot modify checkout session in {terminalStatus} status");
    }

    [Fact]
    public void SetShippingAddress_ShouldExtendExpiration()
    {
        // Arrange
        var session = CreateStartedSession();
        var expiresAtBefore = session.ExpiresAt;

        // Act
        session.SetShippingAddress(CreateTestAddress());

        // Assert
        session.ExpiresAt.Should().BeOnOrAfter(expiresAtBefore);
    }

    #endregion

    #region SetBillingAddress Tests

    [Fact]
    public void SetBillingAddress_SameAsShipping_ShouldUseSameAddress()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        var differentAddress = CreateTestAddress("Different Person");

        // Act
        session.SetBillingAddress(differentAddress, sameAsShipping: true);

        // Assert
        session.BillingSameAsShipping.Should().BeTrue();
        session.BillingAddress.Should().Be(session.ShippingAddress);
    }

    [Fact]
    public void SetBillingAddress_DifferentFromShipping_ShouldUseProvidedAddress()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        var billingAddress = CreateTestAddress("Billing Person");

        // Act
        session.SetBillingAddress(billingAddress, sameAsShipping: false);

        // Assert
        session.BillingSameAsShipping.Should().BeFalse();
        session.BillingAddress.Should().Be(billingAddress);
        session.BillingAddress!.FullName.Should().Be("Billing Person");
    }

    [Fact]
    public void SetBillingAddress_ShouldRaiseCheckoutAddressSetEvent()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        session.ClearDomainEvents();

        // Act
        session.SetBillingAddress(CreateTestAddress(), sameAsShipping: false);

        // Assert
        var addressEvent = session.DomainEvents.OfType<CheckoutAddressSetEvent>().Single();
        addressEvent.AddressType.Should().Be("Billing");
    }

    [Fact]
    public void SetBillingAddress_OnCompletedSession_ShouldThrow()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Act
        var act = () => session.SetBillingAddress(CreateTestAddress(), true);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot modify checkout session in Completed status");
    }

    #endregion

    #region SelectShippingMethod Tests

    [Fact]
    public void SelectShippingMethod_OnAddressCompleteSession_ShouldSetShippingAndTransition()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        var estimatedDelivery = DateTimeOffset.UtcNow.AddDays(5);

        // Act
        session.SelectShippingMethod("Express Delivery", 50000m, estimatedDelivery);

        // Assert
        session.ShippingMethod.Should().Be("Express Delivery");
        session.ShippingCost.Should().Be(50000m);
        session.EstimatedDeliveryAt.Should().Be(estimatedDelivery);
        session.Status.Should().Be(CheckoutSessionStatus.ShippingSelected);
    }

    [Fact]
    public void SelectShippingMethod_ShouldRecalculateGrandTotal()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        session.SubTotal.Should().Be(500000m);

        // Act
        session.SelectShippingMethod("Express", 50000m);

        // Assert
        session.GrandTotal.Should().Be(550000m); // 500,000 + 50,000
    }

    [Fact]
    public void SelectShippingMethod_WithoutAddress_ShouldThrow()
    {
        // Arrange
        var session = CreateStartedSession();

        // Act
        var act = () => session.SelectShippingMethod("Express", 50000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Shipping address must be set before selecting shipping method");
    }

    [Fact]
    public void SelectShippingMethod_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        session.ClearDomainEvents();

        // Act
        session.SelectShippingMethod("Standard", 30000m);

        // Assert
        var statusEvent = session.DomainEvents.OfType<CheckoutSessionStatusChangedEvent>().Single();
        statusEvent.OldStatus.Should().Be(CheckoutSessionStatus.AddressComplete);
        statusEvent.NewStatus.Should().Be(CheckoutSessionStatus.ShippingSelected);
    }

    [Fact]
    public void SelectShippingMethod_ShouldRaiseCheckoutShippingSelectedEvent()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        session.ClearDomainEvents();

        // Act
        session.SelectShippingMethod("Express", 50000m);

        // Assert
        var shippingEvent = session.DomainEvents.OfType<CheckoutShippingSelectedEvent>().Single();
        shippingEvent.SessionId.Should().Be(session.Id);
        shippingEvent.ShippingMethod.Should().Be("Express");
        shippingEvent.ShippingCost.Should().Be(50000m);
    }

    [Fact]
    public void SelectShippingMethod_WithNoEstimatedDelivery_ShouldSetNull()
    {
        // Arrange
        var session = CreateSessionWithAddress();

        // Act
        session.SelectShippingMethod("Standard", 30000m);

        // Assert
        session.EstimatedDeliveryAt.Should().BeNull();
    }

    [Fact]
    public void SelectShippingMethod_OnCompletedSession_ShouldThrow()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Act
        var act = () => session.SelectShippingMethod("Express", 50000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot modify checkout session in Completed status");
    }

    [Fact]
    public void SelectShippingMethod_OnExpiredSession_ShouldThrow()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        session.MarkAsExpired();

        // Act
        var act = () => session.SelectShippingMethod("Express", 50000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot modify checkout session in Expired status");
    }

    #endregion

    #region SelectPaymentMethod Tests

    [Fact]
    public void SelectPaymentMethod_OnShippingSelected_ShouldSetPaymentAndTransition()
    {
        // Arrange
        var session = CreateSessionWithShipping();
        var gatewayId = Guid.NewGuid();

        // Act
        session.SelectPaymentMethod(PaymentMethod.CreditCard, gatewayId);

        // Assert
        session.PaymentMethod.Should().Be(PaymentMethod.CreditCard);
        session.PaymentGatewayId.Should().Be(gatewayId);
        session.Status.Should().Be(CheckoutSessionStatus.PaymentPending);
    }

    [Fact]
    public void SelectPaymentMethod_COD_WithNoGateway_ShouldSucceed()
    {
        // Arrange
        var session = CreateSessionWithShipping();

        // Act
        session.SelectPaymentMethod(PaymentMethod.COD, null);

        // Assert
        session.PaymentMethod.Should().Be(PaymentMethod.COD);
        session.PaymentGatewayId.Should().BeNull();
        session.Status.Should().Be(CheckoutSessionStatus.PaymentPending);
    }

    [Fact]
    public void SelectPaymentMethod_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var session = CreateSessionWithShipping();
        session.ClearDomainEvents();

        // Act
        session.SelectPaymentMethod(PaymentMethod.EWallet, Guid.NewGuid());

        // Assert
        var statusEvent = session.DomainEvents.OfType<CheckoutSessionStatusChangedEvent>().Single();
        statusEvent.OldStatus.Should().Be(CheckoutSessionStatus.ShippingSelected);
        statusEvent.NewStatus.Should().Be(CheckoutSessionStatus.PaymentPending);
    }

    [Fact]
    public void SelectPaymentMethod_OnCompletedSession_ShouldThrow()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Act
        var act = () => session.SelectPaymentMethod(PaymentMethod.COD, null);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot modify checkout session in Completed status");
    }

    [Theory]
    [InlineData(PaymentMethod.EWallet)]
    [InlineData(PaymentMethod.QRCode)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.Installment)]
    [InlineData(PaymentMethod.COD)]
    [InlineData(PaymentMethod.BuyNowPayLater)]
    public void SelectPaymentMethod_AllPaymentTypes_ShouldBeAccepted(PaymentMethod method)
    {
        // Arrange
        var session = CreateSessionWithShipping();

        // Act
        session.SelectPaymentMethod(method, Guid.NewGuid());

        // Assert
        session.PaymentMethod.Should().Be(method);
    }

    #endregion

    #region MarkAsPaymentProcessing Tests

    [Fact]
    public void MarkAsPaymentProcessing_OnPaymentPending_ShouldTransitionToProcessing()
    {
        // Arrange
        var session = CreateSessionWithPayment();

        // Act
        session.MarkAsPaymentProcessing();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.PaymentProcessing);
    }

    [Fact]
    public void MarkAsPaymentProcessing_OnShippingSelected_ShouldSucceed()
    {
        // Arrange
        var session = CreateSessionWithShipping();

        // Act
        session.MarkAsPaymentProcessing();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.PaymentProcessing);
    }

    [Fact]
    public void MarkAsPaymentProcessing_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var session = CreateSessionWithPayment();
        session.ClearDomainEvents();

        // Act
        session.MarkAsPaymentProcessing();

        // Assert
        var statusEvent = session.DomainEvents.OfType<CheckoutSessionStatusChangedEvent>().Single();
        statusEvent.OldStatus.Should().Be(CheckoutSessionStatus.PaymentPending);
        statusEvent.NewStatus.Should().Be(CheckoutSessionStatus.PaymentProcessing);
    }

    [Theory]
    [InlineData(CheckoutSessionStatus.Started)]
    [InlineData(CheckoutSessionStatus.AddressComplete)]
    public void MarkAsPaymentProcessing_OnInvalidStatus_ShouldThrow(CheckoutSessionStatus invalidStatus)
    {
        // Arrange
        CheckoutSession session;
        switch (invalidStatus)
        {
            case CheckoutSessionStatus.Started:
                session = CreateStartedSession();
                break;
            case CheckoutSessionStatus.AddressComplete:
                session = CreateSessionWithAddress();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidStatus));
        }

        // Act
        var act = () => session.MarkAsPaymentProcessing();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot start payment processing in {invalidStatus} status");
    }

    #endregion

    #region Complete Tests

    [Fact]
    public void Complete_OnPaymentProcessing_ShouldTransitionToCompleted()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        var orderId = Guid.NewGuid();
        var orderNumber = "ORD-20260219-0001";

        // Act
        session.Complete(orderId, orderNumber);

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Completed);
        session.OrderId.Should().Be(orderId);
        session.OrderNumber.Should().Be(orderNumber);
    }

    [Fact]
    public void Complete_OnPaymentPending_ShouldTransitionToCompleted()
    {
        // Arrange - COD can complete from PaymentPending directly
        var session = CreateSessionWithPayment();

        // Act
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Completed);
    }

    [Fact]
    public void Complete_ShouldRaiseStatusChangedAndCompletedEvents()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.ClearDomainEvents();
        var orderId = Guid.NewGuid();
        var orderNumber = "ORD-20260219-0001";

        // Act
        session.Complete(orderId, orderNumber);

        // Assert
        session.DomainEvents.Should().HaveCount(2);

        var statusEvent = session.DomainEvents.OfType<CheckoutSessionStatusChangedEvent>().Single();
        statusEvent.OldStatus.Should().Be(CheckoutSessionStatus.PaymentProcessing);
        statusEvent.NewStatus.Should().Be(CheckoutSessionStatus.Completed);

        var completedEvent = session.DomainEvents.OfType<CheckoutCompletedEvent>().Single();
        completedEvent.SessionId.Should().Be(session.Id);
        completedEvent.OrderId.Should().Be(orderId);
        completedEvent.OrderNumber.Should().Be(orderNumber);
        completedEvent.GrandTotal.Should().Be(session.GrandTotal);
    }

    [Theory]
    [InlineData(CheckoutSessionStatus.Started)]
    [InlineData(CheckoutSessionStatus.AddressComplete)]
    [InlineData(CheckoutSessionStatus.ShippingSelected)]
    public void Complete_OnInvalidStatus_ShouldThrow(CheckoutSessionStatus invalidStatus)
    {
        // Arrange
        CheckoutSession session;
        switch (invalidStatus)
        {
            case CheckoutSessionStatus.Started:
                session = CreateStartedSession();
                break;
            case CheckoutSessionStatus.AddressComplete:
                session = CreateSessionWithAddress();
                break;
            case CheckoutSessionStatus.ShippingSelected:
                session = CreateSessionWithShipping();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(invalidStatus));
        }

        // Act
        var act = () => session.Complete(Guid.NewGuid(), "ORD-001");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"Cannot complete checkout in {invalidStatus} status");
    }

    [Fact]
    public void Complete_AlreadyCompleted_ShouldThrow()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Act
        var act = () => session.Complete(Guid.NewGuid(), "ORD-002");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot complete checkout in Completed status");
    }

    #endregion

    #region MarkAsExpired Tests

    [Fact]
    public void MarkAsExpired_OnStartedSession_ShouldTransitionToExpired()
    {
        // Arrange
        var session = CreateStartedSession();

        // Act
        session.MarkAsExpired();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Expired);
    }

    [Fact]
    public void MarkAsExpired_ShouldRaiseStatusChangedAndExpiredEvents()
    {
        // Arrange
        var session = CreateStartedSession();
        session.ClearDomainEvents();

        // Act
        session.MarkAsExpired();

        // Assert
        var statusEvent = session.DomainEvents.OfType<CheckoutSessionStatusChangedEvent>().Single();
        statusEvent.OldStatus.Should().Be(CheckoutSessionStatus.Started);
        statusEvent.NewStatus.Should().Be(CheckoutSessionStatus.Expired);

        var expiredEvent = session.DomainEvents.OfType<CheckoutExpiredEvent>().Single();
        expiredEvent.SessionId.Should().Be(session.Id);
        expiredEvent.CartId.Should().Be(TestCartId);
    }

    [Fact]
    public void MarkAsExpired_OnCompletedSession_ShouldNotChangeStatus()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Act
        session.MarkAsExpired();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Completed);
    }

    [Fact]
    public void MarkAsExpired_AlreadyExpired_ShouldNotRaiseEvent()
    {
        // Arrange
        var session = CreateStartedSession();
        session.MarkAsExpired();
        session.ClearDomainEvents();

        // Act
        session.MarkAsExpired();

        // Assert
        session.DomainEvents.Should().BeEmpty();
    }

    [Theory]
    [InlineData(CheckoutSessionStatus.AddressComplete)]
    [InlineData(CheckoutSessionStatus.ShippingSelected)]
    [InlineData(CheckoutSessionStatus.PaymentPending)]
    [InlineData(CheckoutSessionStatus.PaymentProcessing)]
    public void MarkAsExpired_OnAnyActiveStatus_ShouldTransitionToExpired(CheckoutSessionStatus status)
    {
        // Arrange
        CheckoutSession session;
        switch (status)
        {
            case CheckoutSessionStatus.AddressComplete:
                session = CreateSessionWithAddress();
                break;
            case CheckoutSessionStatus.ShippingSelected:
                session = CreateSessionWithShipping();
                break;
            case CheckoutSessionStatus.PaymentPending:
                session = CreateSessionWithPayment();
                break;
            case CheckoutSessionStatus.PaymentProcessing:
                session = CreateSessionReadyToComplete();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        // Act
        session.MarkAsExpired();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Expired);
    }

    #endregion

    #region MarkAsAbandoned Tests

    [Fact]
    public void MarkAsAbandoned_OnActiveSession_ShouldTransitionToAbandoned()
    {
        // Arrange
        var session = CreateStartedSession();

        // Act
        session.MarkAsAbandoned();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Abandoned);
    }

    [Fact]
    public void MarkAsAbandoned_ShouldRaiseStatusChangedAndAbandonedEvents()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        session.ClearDomainEvents();

        // Act
        session.MarkAsAbandoned();

        // Assert
        var statusEvent = session.DomainEvents.OfType<CheckoutSessionStatusChangedEvent>().Single();
        statusEvent.OldStatus.Should().Be(CheckoutSessionStatus.AddressComplete);
        statusEvent.NewStatus.Should().Be(CheckoutSessionStatus.Abandoned);

        var abandonedEvent = session.DomainEvents.OfType<CheckoutAbandonedEvent>().Single();
        abandonedEvent.SessionId.Should().Be(session.Id);
        abandonedEvent.CartId.Should().Be(TestCartId);
        abandonedEvent.LastStatus.Should().Be(CheckoutSessionStatus.AddressComplete);
    }

    [Fact]
    public void MarkAsAbandoned_OnCompletedSession_ShouldNotChangeStatus()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Act
        session.MarkAsAbandoned();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Completed);
    }

    [Fact]
    public void MarkAsAbandoned_OnExpiredSession_ShouldNotChangeStatus()
    {
        // Arrange
        var session = CreateStartedSession();
        session.MarkAsExpired();

        // Act
        session.MarkAsAbandoned();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Expired);
    }

    [Fact]
    public void MarkAsAbandoned_AlreadyAbandoned_ShouldNotRaiseEvent()
    {
        // Arrange
        var session = CreateStartedSession();
        session.MarkAsAbandoned();
        session.ClearDomainEvents();

        // Act
        session.MarkAsAbandoned();

        // Assert
        session.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region ApplyCoupon Tests

    [Fact]
    public void ApplyCoupon_OnActiveSession_ShouldSetCouponAndDiscount()
    {
        // Arrange
        var session = CreateStartedSession(subTotal: 500000m);

        // Act
        session.ApplyCoupon("SAVE50K", 50000m);

        // Assert
        session.CouponCode.Should().Be("SAVE50K");
        session.DiscountAmount.Should().Be(50000m);
        session.GrandTotal.Should().Be(450000m); // 500,000 - 50,000
    }

    [Fact]
    public void ApplyCoupon_ShouldRecalculateGrandTotalWithShipping()
    {
        // Arrange
        var session = CreateSessionWithShipping(); // 500,000 + 50,000 shipping
        session.GrandTotal.Should().Be(550000m);

        // Act
        session.ApplyCoupon("DISCOUNT", 100000m);

        // Assert
        session.GrandTotal.Should().Be(450000m); // 500,000 - 100,000 + 50,000
    }

    [Fact]
    public void ApplyCoupon_OnCompletedSession_ShouldThrow()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Act
        var act = () => session.ApplyCoupon("CODE", 10000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot modify checkout session in Completed status");
    }

    [Fact]
    public void ApplyCoupon_OnExpiredSession_ShouldThrow()
    {
        // Arrange
        var session = CreateStartedSession();
        session.MarkAsExpired();

        // Act
        var act = () => session.ApplyCoupon("CODE", 10000m);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot modify checkout session in Expired status");
    }

    #endregion

    #region RemoveCoupon Tests

    [Fact]
    public void RemoveCoupon_ShouldClearCouponAndResetDiscount()
    {
        // Arrange
        var session = CreateStartedSession(subTotal: 500000m);
        session.ApplyCoupon("SAVE50K", 50000m);

        // Act
        session.RemoveCoupon();

        // Assert
        session.CouponCode.Should().BeNull();
        session.DiscountAmount.Should().Be(0m);
        session.GrandTotal.Should().Be(500000m);
    }

    [Fact]
    public void RemoveCoupon_WithoutCoupon_ShouldNotThrow()
    {
        // Arrange
        var session = CreateStartedSession();

        // Act
        var act = () => session.RemoveCoupon();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveCoupon_OnCompletedSession_ShouldThrow()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Act
        var act = () => session.RemoveCoupon();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot modify checkout session in Completed status");
    }

    #endregion

    #region SetTax Tests

    [Fact]
    public void SetTax_ShouldUpdateTaxAmountAndRecalculateTotal()
    {
        // Arrange
        var session = CreateStartedSession(subTotal: 500000m);

        // Act
        session.SetTax(50000m);

        // Assert
        session.TaxAmount.Should().Be(50000m);
        session.GrandTotal.Should().Be(550000m); // 500,000 + 50,000
    }

    [Fact]
    public void SetTax_WithShippingAndDiscount_ShouldCalculateCorrectTotal()
    {
        // Arrange
        var session = CreateSessionWithShipping(); // 500,000 subtotal + 50,000 shipping
        session.ApplyCoupon("SAVE", 100000m); // -100,000

        // Act
        session.SetTax(25000m);

        // Assert
        // GrandTotal = SubTotal - Discount + Shipping + Tax = 500,000 - 100,000 + 50,000 + 25,000
        session.GrandTotal.Should().Be(475000m);
    }

    #endregion

    #region SetCustomerNotes Tests

    [Fact]
    public void SetCustomerNotes_ShouldSetNotes()
    {
        // Arrange
        var session = CreateStartedSession();

        // Act
        session.SetCustomerNotes("Please deliver after 6pm");

        // Assert
        session.CustomerNotes.Should().Be("Please deliver after 6pm");
    }

    [Fact]
    public void SetCustomerNotes_WithNull_ShouldClearNotes()
    {
        // Arrange
        var session = CreateStartedSession();
        session.SetCustomerNotes("Some notes");

        // Act
        session.SetCustomerNotes(null);

        // Assert
        session.CustomerNotes.Should().BeNull();
    }

    #endregion

    #region ExtendExpiration Tests

    [Fact]
    public void ExtendExpiration_OnActiveSession_ShouldExtendExpiresAt()
    {
        // Arrange
        var session = CreateStartedSession();
        var beforeExtend = DateTimeOffset.UtcNow;

        // Act
        session.ExtendExpiration(30);

        // Assert
        session.ExpiresAt.Should().BeAfter(beforeExtend.AddMinutes(29));
        session.ExpiresAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddMinutes(30));
    }

    [Fact]
    public void ExtendExpiration_WithDefaultMinutes_ShouldExtend15Minutes()
    {
        // Arrange
        var session = CreateStartedSession();
        var beforeExtend = DateTimeOffset.UtcNow;

        // Act
        session.ExtendExpiration();

        // Assert
        session.ExpiresAt.Should().BeAfter(beforeExtend.AddMinutes(14));
        session.ExpiresAt.Should().BeOnOrBefore(DateTimeOffset.UtcNow.AddMinutes(15));
    }

    [Fact]
    public void ExtendExpiration_OnCompletedSession_ShouldNotExtend()
    {
        // Arrange
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");
        var expiresAtAfterComplete = session.ExpiresAt;

        // Act
        session.ExtendExpiration(60);

        // Assert
        session.ExpiresAt.Should().Be(expiresAtAfterComplete);
    }

    [Fact]
    public void ExtendExpiration_OnExpiredSession_ShouldNotExtend()
    {
        // Arrange
        var session = CreateStartedSession();
        session.MarkAsExpired();
        var expiresAtAfterExpiry = session.ExpiresAt;

        // Act
        session.ExtendExpiration(60);

        // Assert
        session.ExpiresAt.Should().Be(expiresAtAfterExpiry);
    }

    [Fact]
    public void ExtendExpiration_OnAbandonedSession_ShouldNotExtend()
    {
        // Arrange
        var session = CreateStartedSession();
        session.MarkAsAbandoned();
        var expiresAtAfterAbandoned = session.ExpiresAt;

        // Act
        session.ExtendExpiration(60);

        // Assert
        session.ExpiresAt.Should().Be(expiresAtAfterAbandoned);
    }

    #endregion

    #region IsExpired Property Tests

    [Fact]
    public void IsExpired_NewSession_ShouldNotBeExpired()
    {
        // Act
        var session = CreateStartedSession();

        // Assert
        session.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_CompletedSession_ShouldReturnFalse()
    {
        // Arrange - Completed sessions are not considered expired even if time has passed
        var session = CreateSessionReadyToComplete();
        session.Complete(Guid.NewGuid(), "ORD-001");

        // Assert
        session.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_AlreadyMarkedExpired_ShouldReturnFalse()
    {
        // Arrange - Already marked expired means IsExpired should return false
        // (because the status check excludes Expired status)
        var session = CreateStartedSession();
        session.MarkAsExpired();

        // Assert
        session.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_AbandonedSession_ShouldReturnFalse()
    {
        // Arrange
        var session = CreateStartedSession();
        session.MarkAsAbandoned();

        // Assert
        session.IsExpired.Should().BeFalse();
    }

    #endregion

    #region Grand Total Calculation Tests

    [Fact]
    public void GrandTotal_InitiallyEqualsSubTotal()
    {
        // Act
        var session = CreateStartedSession(subTotal: 1000000m);

        // Assert
        session.GrandTotal.Should().Be(1000000m);
    }

    [Fact]
    public void GrandTotal_WithAllComponents_ShouldCalculateCorrectly()
    {
        // Arrange
        var session = CreateSessionWithAddress();

        // Act - Add shipping
        session.SelectShippingMethod("Express", 50000m);

        // Apply discount
        session.ApplyCoupon("SAVE", 100000m);

        // Add tax
        session.SetTax(25000m);

        // Assert
        // GrandTotal = SubTotal(500,000) - Discount(100,000) + Shipping(50,000) + Tax(25,000) = 475,000
        session.GrandTotal.Should().Be(475000m);
    }

    [Fact]
    public void GrandTotal_DiscountExceedsSubTotal_ShouldAllowNegative()
    {
        // Arrange - The domain does not prevent discount > subtotal
        var session = CreateStartedSession(subTotal: 100000m);

        // Act
        session.ApplyCoupon("MEGA_DISCOUNT", 200000m);

        // Assert
        session.GrandTotal.Should().Be(-100000m); // 100,000 - 200,000
    }

    #endregion

    #region Full Checkout Flow (Integration-Style) Tests

    [Fact]
    public void FullCheckoutFlow_HappyPath_ShouldTransitionThroughAllStatuses()
    {
        // 1. Create session
        var session = CheckoutSession.Create(TestCartId, TestEmail, 500000m, TestCurrency, TestUserId, TestTenantId);
        session.Status.Should().Be(CheckoutSessionStatus.Started);

        // 2. Set customer info
        session.SetCustomerInfo("Nguyen Van A", "0901234567");
        session.CustomerName.Should().Be("Nguyen Van A");

        // 3. Set shipping address
        session.SetShippingAddress(CreateTestAddress());
        session.Status.Should().Be(CheckoutSessionStatus.AddressComplete);

        // 4. Select shipping method
        session.SelectShippingMethod("Express Delivery", 50000m, DateTimeOffset.UtcNow.AddDays(3));
        session.Status.Should().Be(CheckoutSessionStatus.ShippingSelected);
        session.GrandTotal.Should().Be(550000m);

        // 5. Apply coupon
        session.ApplyCoupon("WELCOME10", 50000m);
        session.GrandTotal.Should().Be(500000m);

        // 6. Set tax
        session.SetTax(50000m);
        session.GrandTotal.Should().Be(550000m);

        // 7. Select payment method
        session.SelectPaymentMethod(PaymentMethod.CreditCard, Guid.NewGuid());
        session.Status.Should().Be(CheckoutSessionStatus.PaymentPending);

        // 8. Set customer notes
        session.SetCustomerNotes("Leave at front door");

        // 9. Start payment processing
        session.MarkAsPaymentProcessing();
        session.Status.Should().Be(CheckoutSessionStatus.PaymentProcessing);

        // 10. Complete checkout
        var orderId = Guid.NewGuid();
        session.Complete(orderId, "ORD-20260219-0001");
        session.Status.Should().Be(CheckoutSessionStatus.Completed);
        session.OrderId.Should().Be(orderId);
        session.OrderNumber.Should().Be("ORD-20260219-0001");
    }

    [Fact]
    public void CheckoutFlow_AbandonedDuringShipping_ShouldTransitionToAbandoned()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        session.Status.Should().Be(CheckoutSessionStatus.AddressComplete);

        // Act - User abandons during shipping selection
        session.MarkAsAbandoned();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Abandoned);
    }

    [Fact]
    public void CheckoutFlow_ExpiredDuringPayment_ShouldTransitionToExpired()
    {
        // Arrange
        var session = CreateSessionWithPayment();
        session.Status.Should().Be(CheckoutSessionStatus.PaymentPending);

        // Act - Session expires while waiting for payment
        session.MarkAsExpired();

        // Assert
        session.Status.Should().Be(CheckoutSessionStatus.Expired);
    }

    #endregion

    #region Activity Tracking Tests

    [Fact]
    public void SetShippingAddress_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var session = CreateStartedSession();
        var beforeAction = DateTimeOffset.UtcNow;

        // Act
        session.SetShippingAddress(CreateTestAddress());

        // Assert
        session.LastActivityAt.Should().BeOnOrAfter(beforeAction);
    }

    [Fact]
    public void SelectShippingMethod_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var session = CreateSessionWithAddress();
        var beforeAction = DateTimeOffset.UtcNow;

        // Act
        session.SelectShippingMethod("Standard", 30000m);

        // Assert
        session.LastActivityAt.Should().BeOnOrAfter(beforeAction);
    }

    [Fact]
    public void SelectPaymentMethod_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var session = CreateSessionWithShipping();
        var beforeAction = DateTimeOffset.UtcNow;

        // Act
        session.SelectPaymentMethod(PaymentMethod.COD, null);

        // Assert
        session.LastActivityAt.Should().BeOnOrAfter(beforeAction);
    }

    [Fact]
    public void ApplyCoupon_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var session = CreateStartedSession();
        var beforeAction = DateTimeOffset.UtcNow;

        // Act
        session.ApplyCoupon("CODE", 10000m);

        // Assert
        session.LastActivityAt.Should().BeOnOrAfter(beforeAction);
    }

    [Fact]
    public void SetCustomerNotes_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var session = CreateStartedSession();
        var beforeAction = DateTimeOffset.UtcNow;

        // Act
        session.SetCustomerNotes("Notes");

        // Assert
        session.LastActivityAt.Should().BeOnOrAfter(beforeAction);
    }

    #endregion
}
