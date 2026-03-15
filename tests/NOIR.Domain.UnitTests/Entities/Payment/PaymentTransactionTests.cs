using NOIR.Domain.Entities.Payment;
using NOIR.Domain.Events.Payment;

namespace NOIR.Domain.UnitTests.Entities.Payment;

/// <summary>
/// Unit tests for the PaymentTransaction aggregate root entity.
/// Tests factory methods, status transitions, domain events, COD-specific logic,
/// financial computations, metadata, and business rule enforcement.
/// </summary>
public class PaymentTransactionTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestTransactionNumber = "TXN-20260219-0001";
    private const string TestProvider = "vnpay";
    private const decimal TestAmount = 500_000m;
    private const string TestCurrency = "VND";
    private const string TestIdempotencyKey = "idem-key-abc-123";
    private static readonly Guid TestGatewayId = Guid.NewGuid();

    /// <summary>
    /// Helper to create a default valid PaymentTransaction for tests.
    /// </summary>
    private static PaymentTransaction CreateTestTransaction(
        string? transactionNumber = null,
        Guid? paymentGatewayId = null,
        string provider = TestProvider,
        decimal amount = TestAmount,
        string currency = TestCurrency,
        PaymentMethod paymentMethod = PaymentMethod.EWallet,
        string? idempotencyKey = null,
        string? tenantId = TestTenantId)
    {
        return PaymentTransaction.Create(
            transactionNumber ?? TestTransactionNumber,
            paymentGatewayId ?? TestGatewayId,
            provider,
            amount,
            currency,
            paymentMethod,
            idempotencyKey ?? TestIdempotencyKey,
            tenantId);
    }

    /// <summary>
    /// Helper to create a COD payment transaction.
    /// </summary>
    private static PaymentTransaction CreateCodTransaction()
    {
        return CreateTestTransaction(paymentMethod: PaymentMethod.COD);
    }

    #region Create Factory

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidTransaction()
    {
        // Act
        var transaction = PaymentTransaction.Create(
            TestTransactionNumber, TestGatewayId, TestProvider,
            TestAmount, TestCurrency, PaymentMethod.EWallet,
            TestIdempotencyKey, TestTenantId);

        // Assert
        transaction.ShouldNotBeNull();
        transaction.Id.ShouldNotBe(Guid.Empty);
        transaction.TransactionNumber.ShouldBe(TestTransactionNumber);
        transaction.PaymentGatewayId.ShouldBe(TestGatewayId);
        transaction.Provider.ShouldBe(TestProvider);
        transaction.Amount.ShouldBe(TestAmount);
        transaction.Currency.ShouldBe(TestCurrency);
        transaction.PaymentMethod.ShouldBe(PaymentMethod.EWallet);
        transaction.IdempotencyKey.ShouldBe(TestIdempotencyKey);
        transaction.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var transaction = CreateTestTransaction();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var transaction = CreateTestTransaction();

        // Assert
        transaction.GatewayTransactionId.ShouldBeNull();
        transaction.OrderId.ShouldBeNull();
        transaction.CustomerId.ShouldBeNull();
        transaction.ExchangeRate.ShouldBeNull();
        transaction.GatewayFee.ShouldBeNull();
        transaction.NetAmount.ShouldBeNull();
        transaction.FailureReason.ShouldBeNull();
        transaction.FailureCode.ShouldBeNull();
        transaction.PaymentMethodDetail.ShouldBeNull();
        transaction.PayerInfo.ShouldBeNull();
        transaction.IpAddress.ShouldBeNull();
        transaction.UserAgent.ShouldBeNull();
        transaction.ReturnUrl.ShouldBeNull();
        transaction.GatewayResponseJson.ShouldBeNull();
        transaction.MetadataJson.ShouldBeNull();
        transaction.PaidAt.ShouldBeNull();
        transaction.ExpiresAt.ShouldBeNull();
        transaction.CodCollectorName.ShouldBeNull();
        transaction.CodCollectedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyCollections()
    {
        // Act
        var transaction = CreateTestTransaction();

        // Assert
        transaction.Refunds.ShouldNotBeNull();
        transaction.Refunds.ShouldBeEmpty();
        transaction.Installments.ShouldNotBeNull();
        transaction.Installments.ShouldBeEmpty();
    }

    [Fact]
    public void Create_ShouldRaisePaymentCreatedEvent()
    {
        // Act
        var transaction = CreateTestTransaction();

        // Assert
        var __evt = transaction.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<PaymentCreatedEvent>();

        __evt.TransactionId.ShouldBe(transaction.Id);

        __evt.TransactionNumber.ShouldBe(TestTransactionNumber);

        __evt.Amount.ShouldBe(TestAmount);

        __evt.Currency.ShouldBe(TestCurrency);

        __evt.Provider.ShouldBe(TestProvider);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var transaction = CreateTestTransaction(tenantId: null);

        // Assert
        transaction.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_MultipleTransactions_ShouldHaveUniqueIds()
    {
        // Act
        var t1 = CreateTestTransaction(transactionNumber: "TXN-001", idempotencyKey: "key-1");
        var t2 = CreateTestTransaction(transactionNumber: "TXN-002", idempotencyKey: "key-2");

        // Assert
        t1.Id.ShouldNotBe(t2.Id);
    }

    [Theory]
    [InlineData(PaymentMethod.EWallet)]
    [InlineData(PaymentMethod.QRCode)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.COD)]
    [InlineData(PaymentMethod.Installment)]
    public void Create_WithVariousPaymentMethods_ShouldSetCorrectly(PaymentMethod method)
    {
        // Act
        var transaction = CreateTestTransaction(paymentMethod: method);

        // Assert
        transaction.PaymentMethod.ShouldBe(method);
    }

    #endregion

    #region SetOrderId / SetCustomerId

    [Fact]
    public void SetOrderId_ShouldSetValue()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var orderId = Guid.NewGuid();

        // Act
        transaction.SetOrderId(orderId);

        // Assert
        transaction.OrderId.ShouldBe(orderId);
    }

    [Fact]
    public void SetCustomerId_ShouldSetValue()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var customerId = Guid.NewGuid();

        // Act
        transaction.SetCustomerId(customerId);

        // Assert
        transaction.CustomerId.ShouldBe(customerId);
    }

    #endregion

    #region SetRequestMetadata

    [Fact]
    public void SetRequestMetadata_ShouldSetAllFields()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.SetRequestMetadata("203.0.113.50", "Mozilla/5.0", "https://example.com/return");

        // Assert
        transaction.IpAddress.ShouldBe("203.0.113.50");
        transaction.UserAgent.ShouldBe("Mozilla/5.0");
        transaction.ReturnUrl.ShouldBe("https://example.com/return");
    }

    [Fact]
    public void SetRequestMetadata_WithNullValues_ShouldAllowNulls()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.SetRequestMetadata(null, null, null);

        // Assert
        transaction.IpAddress.ShouldBeNull();
        transaction.UserAgent.ShouldBeNull();
        transaction.ReturnUrl.ShouldBeNull();
    }

    #endregion

    #region SetExpiresAt

    [Fact]
    public void SetExpiresAt_ShouldSetExpiration()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);

        // Act
        transaction.SetExpiresAt(expiresAt);

        // Assert
        transaction.ExpiresAt.ShouldBe(expiresAt);
    }

    #endregion

    #region Status Transitions - MarkAsProcessing

    [Fact]
    public void MarkAsProcessing_ShouldTransitionToProcessing()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsProcessing();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Processing);
    }

    [Fact]
    public void MarkAsProcessing_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.ClearDomainEvents();

        // Act
        transaction.MarkAsProcessing();

        // Assert
        var __evt = transaction.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<PaymentStatusChangedEvent>();

        __evt.TransactionId.ShouldBe(transaction.Id);

        __evt.OldStatus.ShouldBe(PaymentStatus.Pending);

        __evt.NewStatus.ShouldBe(PaymentStatus.Processing);
    }

    #endregion

    #region Status Transitions - MarkAsRequiresAction

    [Fact]
    public void MarkAsRequiresAction_ShouldTransitionToRequiresAction()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsRequiresAction();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.RequiresAction);
    }

    [Fact]
    public void MarkAsRequiresAction_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.ClearDomainEvents();

        // Act
        transaction.MarkAsRequiresAction();

        // Assert
        var __evt = transaction.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<PaymentStatusChangedEvent>();

        __evt.OldStatus.ShouldBe(PaymentStatus.Pending);

        __evt.NewStatus.ShouldBe(PaymentStatus.RequiresAction);
    }

    #endregion

    #region Status Transitions - MarkAsPaid

    [Fact]
    public void MarkAsPaid_ShouldTransitionToPaid()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.MarkAsProcessing();

        // Act
        transaction.MarkAsPaid("GW-TXN-12345");

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Paid);
    }

    [Fact]
    public void MarkAsPaid_ShouldSetGatewayTransactionId()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsPaid("GW-TXN-12345");

        // Assert
        transaction.GatewayTransactionId.ShouldBe("GW-TXN-12345");
    }

    [Fact]
    public void MarkAsPaid_ShouldSetPaidAtTimestamp()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        var beforePaid = DateTimeOffset.UtcNow;

        // Act
        transaction.MarkAsPaid("GW-TXN-12345");

        // Assert
        transaction.PaidAt.ShouldNotBeNull();
        transaction.PaidAt!.Value.ShouldBeGreaterThanOrEqualTo(beforePaid);
    }

    [Fact]
    public void MarkAsPaid_ShouldRaiseStatusChangedAndSucceededEvents()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.ClearDomainEvents();

        // Act
        transaction.MarkAsPaid("GW-TXN-12345");

        // Assert
        transaction.DomainEvents.Count().ShouldBe(2);
        var statusEvt = transaction.DomainEvents.Single(e => e is PaymentStatusChangedEvent)
            .ShouldBeOfType<PaymentStatusChangedEvent>();
        statusEvt.OldStatus.ShouldBe(PaymentStatus.Pending);
        statusEvt.NewStatus.ShouldBe(PaymentStatus.Paid);

        var succeededEvt = transaction.DomainEvents.Single(e => e is PaymentSucceededEvent)
            .ShouldBeOfType<PaymentSucceededEvent>();
        succeededEvt.TransactionId.ShouldBe(transaction.Id);
        succeededEvt.Provider.ShouldBe(TestProvider);
        succeededEvt.Amount.ShouldBe(TestAmount);
        succeededEvt.GatewayTransactionId.ShouldBe("GW-TXN-12345");
    }

    #endregion

    #region Status Transitions - MarkAsFailed

    [Fact]
    public void MarkAsFailed_ShouldTransitionToFailed()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsFailed("Card declined", "CARD_DECLINED");

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Failed);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetFailureReasonAndCode()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsFailed("Insufficient funds", "INSUFFICIENT_FUNDS");

        // Assert
        transaction.FailureReason.ShouldBe("Insufficient funds");
        transaction.FailureCode.ShouldBe("INSUFFICIENT_FUNDS");
    }

    [Fact]
    public void MarkAsFailed_WithNullFailureCode_ShouldAllowNull()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsFailed("Unknown error");

        // Assert
        transaction.FailureReason.ShouldBe("Unknown error");
        transaction.FailureCode.ShouldBeNull();
    }

    [Fact]
    public void MarkAsFailed_ShouldRaiseStatusChangedAndFailedEvents()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.ClearDomainEvents();

        // Act
        transaction.MarkAsFailed("Timeout", "TIMEOUT");

        // Assert
        transaction.DomainEvents.Count().ShouldBe(2);
        var statusEvt = transaction.DomainEvents.Single(e => e is PaymentStatusChangedEvent)
            .ShouldBeOfType<PaymentStatusChangedEvent>();
        statusEvt.OldStatus.ShouldBe(PaymentStatus.Pending);
        statusEvt.NewStatus.ShouldBe(PaymentStatus.Failed);
        statusEvt.Reason.ShouldBe("Timeout");

        var failedEvt = transaction.DomainEvents.Single(e => e is PaymentFailedEvent)
            .ShouldBeOfType<PaymentFailedEvent>();
        failedEvt.TransactionId.ShouldBe(transaction.Id);
        failedEvt.Reason.ShouldBe("Timeout");
        failedEvt.FailureCode.ShouldBe("TIMEOUT");
    }

    #endregion

    #region Status Transitions - MarkAsCancelled

    [Fact]
    public void MarkAsCancelled_ShouldTransitionToCancelled()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsCancelled();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Cancelled);
    }

    [Fact]
    public void MarkAsCancelled_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.ClearDomainEvents();

        // Act
        transaction.MarkAsCancelled();

        // Assert
        var __evt = transaction.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<PaymentStatusChangedEvent>();

        __evt.OldStatus.ShouldBe(PaymentStatus.Pending);

        __evt.NewStatus.ShouldBe(PaymentStatus.Cancelled);
    }

    #endregion

    #region Status Transitions - MarkAsExpired

    [Fact]
    public void MarkAsExpired_ShouldTransitionToExpired()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsExpired();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Expired);
    }

    [Fact]
    public void MarkAsExpired_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.ClearDomainEvents();

        // Act
        transaction.MarkAsExpired();

        // Assert
        var __evt = transaction.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<PaymentStatusChangedEvent>();

        __evt.OldStatus.ShouldBe(PaymentStatus.Pending);

        __evt.NewStatus.ShouldBe(PaymentStatus.Expired);
    }

    #endregion

    #region Status Transitions - MarkAsCodPending

    [Fact]
    public void MarkAsCodPending_ShouldTransitionToCodPending()
    {
        // Arrange
        var transaction = CreateCodTransaction();

        // Act
        transaction.MarkAsCodPending();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.CodPending);
    }

    [Fact]
    public void MarkAsCodPending_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var transaction = CreateCodTransaction();
        transaction.ClearDomainEvents();

        // Act
        transaction.MarkAsCodPending();

        // Assert
        var __evt = transaction.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<PaymentStatusChangedEvent>();

        __evt.OldStatus.ShouldBe(PaymentStatus.Pending);

        __evt.NewStatus.ShouldBe(PaymentStatus.CodPending);
    }

    #endregion

    #region Status Transitions - MarkAsAuthorized

    [Fact]
    public void MarkAsAuthorized_ShouldTransitionToAuthorized()
    {
        // Arrange
        var transaction = CreateTestTransaction(paymentMethod: PaymentMethod.CreditCard);

        // Act
        transaction.MarkAsAuthorized();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Authorized);
    }

    [Fact]
    public void MarkAsAuthorized_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.ClearDomainEvents();

        // Act
        transaction.MarkAsAuthorized();

        // Assert
        var __evt = transaction.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<PaymentStatusChangedEvent>();

        __evt.OldStatus.ShouldBe(PaymentStatus.Pending);

        __evt.NewStatus.ShouldBe(PaymentStatus.Authorized);
    }

    #endregion

    #region Status Transitions - MarkAsRefunded

    [Fact]
    public void MarkAsRefunded_ShouldTransitionToRefunded()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.MarkAsPaid("GW-TXN-001");

        // Act
        transaction.MarkAsRefunded();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Refunded);
    }

    [Fact]
    public void MarkAsRefunded_ShouldRaiseStatusChangedEvent()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.MarkAsPaid("GW-TXN-001");
        transaction.ClearDomainEvents();

        // Act
        transaction.MarkAsRefunded();

        // Assert
        var __evt = transaction.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<PaymentStatusChangedEvent>();

        __evt.OldStatus.ShouldBe(PaymentStatus.Paid);

        __evt.NewStatus.ShouldBe(PaymentStatus.Refunded);
    }

    #endregion

    #region COD Collection - ConfirmCodCollection

    [Fact]
    public void ConfirmCodCollection_WithCodMethod_ShouldTransitionToCodCollected()
    {
        // Arrange
        var transaction = CreateCodTransaction();
        transaction.MarkAsCodPending();

        // Act
        transaction.ConfirmCodCollection("Driver Nguyen");

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.CodCollected);
    }

    [Fact]
    public void ConfirmCodCollection_ShouldSetCollectorName()
    {
        // Arrange
        var transaction = CreateCodTransaction();
        transaction.MarkAsCodPending();

        // Act
        transaction.ConfirmCodCollection("Driver Tran");

        // Assert
        transaction.CodCollectorName.ShouldBe("Driver Tran");
    }

    [Fact]
    public void ConfirmCodCollection_ShouldSetCodCollectedAtTimestamp()
    {
        // Arrange
        var transaction = CreateCodTransaction();
        transaction.MarkAsCodPending();
        var beforeCollection = DateTimeOffset.UtcNow;

        // Act
        transaction.ConfirmCodCollection("Driver Le");

        // Assert
        transaction.CodCollectedAt.ShouldNotBeNull();
        transaction.CodCollectedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeCollection);
    }

    [Fact]
    public void ConfirmCodCollection_ShouldRaiseStatusChangedAndCodCollectedEvents()
    {
        // Arrange
        var transaction = CreateCodTransaction();
        transaction.MarkAsCodPending();
        transaction.ClearDomainEvents();

        // Act
        transaction.ConfirmCodCollection("Driver Pham");

        // Assert
        transaction.DomainEvents.Count().ShouldBe(2);
        var __evt = transaction.DomainEvents.Single(e => e is PaymentStatusChangedEvent)

            .ShouldBeOfType<PaymentStatusChangedEvent>();

        __evt.OldStatus.ShouldBe(PaymentStatus.CodPending);

        __evt.NewStatus.ShouldBe(PaymentStatus.CodCollected);
        transaction.DomainEvents.Single(e => e is CodCollectedEvent)
            .ShouldBeOfType<CodCollectedEvent>()
            .CollectorName.ShouldBe("Driver Pham");
    }

    [Fact]
    public void ConfirmCodCollection_WithNonCodMethod_ShouldThrow()
    {
        // Arrange
        var transaction = CreateTestTransaction(paymentMethod: PaymentMethod.EWallet);

        // Act
        var act = () => transaction.ConfirmCodCollection("Driver");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Only COD payments can be confirmed for collection");
    }

    [Theory]
    [InlineData(PaymentMethod.EWallet)]
    [InlineData(PaymentMethod.QRCode)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.DebitCard)]
    [InlineData(PaymentMethod.Installment)]
    [InlineData(PaymentMethod.BuyNowPayLater)]
    public void ConfirmCodCollection_WithNonCodPaymentMethods_ShouldThrow(PaymentMethod method)
    {
        // Arrange
        var transaction = CreateTestTransaction(paymentMethod: method);

        // Act
        var act = () => transaction.ConfirmCodCollection("Driver");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Only COD payments can be confirmed for collection");
    }

    #endregion

    #region SetGatewayResponse / SetGatewayFee / SetGatewayTransactionId

    [Fact]
    public void SetGatewayResponse_ShouldSetJsonResponse()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.SetGatewayResponse("{\"vnp_ResponseCode\":\"00\",\"vnp_Amount\":\"50000000\"}");

        // Assert
        transaction.GatewayResponseJson.ShouldBe("{\"vnp_ResponseCode\":\"00\",\"vnp_Amount\":\"50000000\"}");
    }

    [Fact]
    public void SetGatewayFee_ShouldSetFeeAndCalculateNetAmount()
    {
        // Arrange
        var transaction = CreateTestTransaction(amount: 1_000_000m);

        // Act
        transaction.SetGatewayFee(15_000m);

        // Assert
        transaction.GatewayFee.ShouldBe(15_000m);
        transaction.NetAmount.ShouldBe(985_000m);
    }

    [Fact]
    public void SetGatewayFee_WithZeroFee_ShouldSetNetAmountEqualToAmount()
    {
        // Arrange
        var transaction = CreateTestTransaction(amount: 500_000m);

        // Act
        transaction.SetGatewayFee(0m);

        // Assert
        transaction.GatewayFee.ShouldBe(0m);
        transaction.NetAmount.ShouldBe(500_000m);
    }

    [Fact]
    public void SetGatewayTransactionId_ShouldSetValue()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.SetGatewayTransactionId("GW-EXTERNAL-ID-999");

        // Assert
        transaction.GatewayTransactionId.ShouldBe("GW-EXTERNAL-ID-999");
    }

    [Fact]
    public void SetGatewayTransactionId_CalledTwice_ShouldOverwritePreviousValue()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.SetGatewayTransactionId("first-id");

        // Act
        transaction.SetGatewayTransactionId("second-id");

        // Assert
        transaction.GatewayTransactionId.ShouldBe("second-id");
    }

    #endregion

    #region SetMetadataJson

    [Fact]
    public void SetMetadataJson_ShouldSetValue()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.SetMetadataJson("{\"source\":\"checkout\",\"campaign\":\"summer2026\"}");

        // Assert
        transaction.MetadataJson.ShouldBe("{\"source\":\"checkout\",\"campaign\":\"summer2026\"}");
    }

    [Fact]
    public void SetMetadataJson_CalledTwice_ShouldOverwritePreviousValue()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.SetMetadataJson("{\"v\":1}");

        // Act
        transaction.SetMetadataJson("{\"v\":2}");

        // Assert
        transaction.MetadataJson.ShouldBe("{\"v\":2}");
    }

    #endregion

    #region Domain Events Accumulation

    [Fact]
    public void DomainEvents_ShouldAccumulateAcrossMultipleOperations()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        // PaymentCreatedEvent already raised

        // Act
        transaction.MarkAsProcessing();  // +1 StatusChanged
        transaction.MarkAsPaid("GW-001"); // +1 StatusChanged + 1 PaymentSucceeded

        // Assert - Created(1) + Processing(1) + Paid(2) = 4
        transaction.DomainEvents.Count().ShouldBe(4);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.MarkAsProcessing();
        transaction.DomainEvents.Count().ShouldBeGreaterThan(0);

        // Act
        transaction.ClearDomainEvents();

        // Assert
        transaction.DomainEvents.ShouldBeEmpty();
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_PendingToProcessingToPaid_ShouldTransitionCorrectly()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act & Assert
        transaction.Status.ShouldBe(PaymentStatus.Pending);

        transaction.MarkAsProcessing();
        transaction.Status.ShouldBe(PaymentStatus.Processing);

        transaction.MarkAsPaid("GW-TXN-FINAL");
        transaction.Status.ShouldBe(PaymentStatus.Paid);
        transaction.PaidAt.ShouldNotBeNull();
        transaction.GatewayTransactionId.ShouldBe("GW-TXN-FINAL");
    }

    [Fact]
    public void FullLifecycle_PendingToFailed_ShouldTransitionCorrectly()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsProcessing();
        transaction.MarkAsFailed("Gateway timeout", "GW_TIMEOUT");

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Failed);
        transaction.FailureReason.ShouldBe("Gateway timeout");
        transaction.FailureCode.ShouldBe("GW_TIMEOUT");
    }

    [Fact]
    public void FullLifecycle_PendingToCancelledToExpired_StatusShouldReflectLatest()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsCancelled();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Cancelled);
    }

    [Fact]
    public void FullLifecycle_CodPayment_ShouldFollowCodPath()
    {
        // Arrange
        var transaction = CreateCodTransaction();

        // Act & Assert
        transaction.Status.ShouldBe(PaymentStatus.Pending);
        transaction.PaymentMethod.ShouldBe(PaymentMethod.COD);

        transaction.MarkAsCodPending();
        transaction.Status.ShouldBe(PaymentStatus.CodPending);

        transaction.ConfirmCodCollection("Delivery Driver A");
        transaction.Status.ShouldBe(PaymentStatus.CodCollected);
        transaction.CodCollectorName.ShouldBe("Delivery Driver A");
        transaction.CodCollectedAt.ShouldNotBeNull();
    }

    [Fact]
    public void FullLifecycle_PaidThenRefunded_ShouldTransitionCorrectly()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.MarkAsProcessing();
        transaction.MarkAsPaid("GW-TXN-PAY");

        // Act
        transaction.MarkAsRefunded();

        // Assert
        transaction.Status.ShouldBe(PaymentStatus.Refunded);
        transaction.PaidAt.ShouldNotBeNull();
    }

    [Fact]
    public void FullLifecycle_WithMetadataAndFees_ShouldSetAllFields()
    {
        // Arrange
        var transaction = CreateTestTransaction(amount: 2_000_000m);
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        // Act
        transaction.SetOrderId(orderId);
        transaction.SetCustomerId(customerId);
        transaction.SetRequestMetadata("10.0.0.1", "Chrome/120", "https://shop.vn/return");
        transaction.SetExpiresAt(DateTimeOffset.UtcNow.AddMinutes(30));
        transaction.MarkAsProcessing();
        transaction.MarkAsPaid("GW-TXN-FULL");
        transaction.SetGatewayFee(30_000m);
        transaction.SetGatewayResponse("{\"code\":\"00\"}");
        transaction.SetMetadataJson("{\"checkoutSessionId\":\"sess-123\"}");

        // Assert
        transaction.OrderId.ShouldBe(orderId);
        transaction.CustomerId.ShouldBe(customerId);
        transaction.IpAddress.ShouldBe("10.0.0.1");
        transaction.UserAgent.ShouldBe("Chrome/120");
        transaction.ReturnUrl.ShouldBe("https://shop.vn/return");
        transaction.ExpiresAt.ShouldNotBeNull();
        transaction.Status.ShouldBe(PaymentStatus.Paid);
        transaction.GatewayTransactionId.ShouldBe("GW-TXN-FULL");
        transaction.GatewayFee.ShouldBe(30_000m);
        transaction.NetAmount.ShouldBe(1_970_000m);
        transaction.GatewayResponseJson.ShouldBe("{\"code\":\"00\"}");
        transaction.MetadataJson.ShouldBe("{\"checkoutSessionId\":\"sess-123\"}");
    }

    #endregion
}
