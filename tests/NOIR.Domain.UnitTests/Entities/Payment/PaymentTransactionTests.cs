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
        transaction.Should().NotBeNull();
        transaction.Id.Should().NotBe(Guid.Empty);
        transaction.TransactionNumber.Should().Be(TestTransactionNumber);
        transaction.PaymentGatewayId.Should().Be(TestGatewayId);
        transaction.Provider.Should().Be(TestProvider);
        transaction.Amount.Should().Be(TestAmount);
        transaction.Currency.Should().Be(TestCurrency);
        transaction.PaymentMethod.Should().Be(PaymentMethod.EWallet);
        transaction.IdempotencyKey.Should().Be(TestIdempotencyKey);
        transaction.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var transaction = CreateTestTransaction();

        // Assert
        transaction.Status.Should().Be(PaymentStatus.Pending);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var transaction = CreateTestTransaction();

        // Assert
        transaction.GatewayTransactionId.Should().BeNull();
        transaction.OrderId.Should().BeNull();
        transaction.CustomerId.Should().BeNull();
        transaction.ExchangeRate.Should().BeNull();
        transaction.GatewayFee.Should().BeNull();
        transaction.NetAmount.Should().BeNull();
        transaction.FailureReason.Should().BeNull();
        transaction.FailureCode.Should().BeNull();
        transaction.PaymentMethodDetail.Should().BeNull();
        transaction.PayerInfo.Should().BeNull();
        transaction.IpAddress.Should().BeNull();
        transaction.UserAgent.Should().BeNull();
        transaction.ReturnUrl.Should().BeNull();
        transaction.GatewayResponseJson.Should().BeNull();
        transaction.MetadataJson.Should().BeNull();
        transaction.PaidAt.Should().BeNull();
        transaction.ExpiresAt.Should().BeNull();
        transaction.CodCollectorName.Should().BeNull();
        transaction.CodCollectedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeEmptyCollections()
    {
        // Act
        var transaction = CreateTestTransaction();

        // Assert
        transaction.Refunds.Should().NotBeNull();
        transaction.Refunds.Should().BeEmpty();
        transaction.Installments.Should().NotBeNull();
        transaction.Installments.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldRaisePaymentCreatedEvent()
    {
        // Act
        var transaction = CreateTestTransaction();

        // Assert
        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentCreatedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                TransactionId = transaction.Id,
                TransactionNumber = TestTransactionNumber,
                Amount = TestAmount,
                Currency = TestCurrency,
                Provider = TestProvider
            });
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var transaction = CreateTestTransaction(tenantId: null);

        // Assert
        transaction.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleTransactions_ShouldHaveUniqueIds()
    {
        // Act
        var t1 = CreateTestTransaction(transactionNumber: "TXN-001", idempotencyKey: "key-1");
        var t2 = CreateTestTransaction(transactionNumber: "TXN-002", idempotencyKey: "key-2");

        // Assert
        t1.Id.Should().NotBe(t2.Id);
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
        transaction.PaymentMethod.Should().Be(method);
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
        transaction.OrderId.Should().Be(orderId);
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
        transaction.CustomerId.Should().Be(customerId);
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
        transaction.IpAddress.Should().Be("203.0.113.50");
        transaction.UserAgent.Should().Be("Mozilla/5.0");
        transaction.ReturnUrl.Should().Be("https://example.com/return");
    }

    [Fact]
    public void SetRequestMetadata_WithNullValues_ShouldAllowNulls()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.SetRequestMetadata(null, null, null);

        // Assert
        transaction.IpAddress.Should().BeNull();
        transaction.UserAgent.Should().BeNull();
        transaction.ReturnUrl.Should().BeNull();
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
        transaction.ExpiresAt.Should().Be(expiresAt);
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
        transaction.Status.Should().Be(PaymentStatus.Processing);
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
        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                TransactionId = transaction.Id,
                OldStatus = PaymentStatus.Pending,
                NewStatus = PaymentStatus.Processing
            });
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
        transaction.Status.Should().Be(PaymentStatus.RequiresAction);
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
        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = PaymentStatus.Pending,
                NewStatus = PaymentStatus.RequiresAction
            });
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
        transaction.Status.Should().Be(PaymentStatus.Paid);
    }

    [Fact]
    public void MarkAsPaid_ShouldSetGatewayTransactionId()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsPaid("GW-TXN-12345");

        // Assert
        transaction.GatewayTransactionId.Should().Be("GW-TXN-12345");
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
        transaction.PaidAt.Should().NotBeNull();
        transaction.PaidAt.Should().BeOnOrAfter(beforePaid);
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
        transaction.DomainEvents.Should().HaveCount(2);
        transaction.DomainEvents.Should().ContainSingle(e => e is PaymentStatusChangedEvent)
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = PaymentStatus.Pending,
                NewStatus = PaymentStatus.Paid
            });
        transaction.DomainEvents.Should().ContainSingle(e => e is PaymentSucceededEvent)
            .Which.Should().BeOfType<PaymentSucceededEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                TransactionId = transaction.Id,
                Provider = TestProvider,
                Amount = TestAmount,
                GatewayTransactionId = "GW-TXN-12345"
            });
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
        transaction.Status.Should().Be(PaymentStatus.Failed);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetFailureReasonAndCode()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsFailed("Insufficient funds", "INSUFFICIENT_FUNDS");

        // Assert
        transaction.FailureReason.Should().Be("Insufficient funds");
        transaction.FailureCode.Should().Be("INSUFFICIENT_FUNDS");
    }

    [Fact]
    public void MarkAsFailed_WithNullFailureCode_ShouldAllowNull()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsFailed("Unknown error");

        // Assert
        transaction.FailureReason.Should().Be("Unknown error");
        transaction.FailureCode.Should().BeNull();
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
        transaction.DomainEvents.Should().HaveCount(2);
        transaction.DomainEvents.Should().ContainSingle(e => e is PaymentStatusChangedEvent)
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = PaymentStatus.Pending,
                NewStatus = PaymentStatus.Failed,
                Reason = "Timeout"
            });
        transaction.DomainEvents.Should().ContainSingle(e => e is PaymentFailedEvent)
            .Which.Should().BeOfType<PaymentFailedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                TransactionId = transaction.Id,
                Reason = "Timeout",
                FailureCode = "TIMEOUT"
            });
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
        transaction.Status.Should().Be(PaymentStatus.Cancelled);
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
        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = PaymentStatus.Pending,
                NewStatus = PaymentStatus.Cancelled
            });
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
        transaction.Status.Should().Be(PaymentStatus.Expired);
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
        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = PaymentStatus.Pending,
                NewStatus = PaymentStatus.Expired
            });
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
        transaction.Status.Should().Be(PaymentStatus.CodPending);
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
        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = PaymentStatus.Pending,
                NewStatus = PaymentStatus.CodPending
            });
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
        transaction.Status.Should().Be(PaymentStatus.Authorized);
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
        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = PaymentStatus.Pending,
                NewStatus = PaymentStatus.Authorized
            });
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
        transaction.Status.Should().Be(PaymentStatus.Refunded);
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
        transaction.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = PaymentStatus.Paid,
                NewStatus = PaymentStatus.Refunded
            });
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
        transaction.Status.Should().Be(PaymentStatus.CodCollected);
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
        transaction.CodCollectorName.Should().Be("Driver Tran");
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
        transaction.CodCollectedAt.Should().NotBeNull();
        transaction.CodCollectedAt.Should().BeOnOrAfter(beforeCollection);
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
        transaction.DomainEvents.Should().HaveCount(2);
        transaction.DomainEvents.Should().ContainSingle(e => e is PaymentStatusChangedEvent)
            .Which.Should().BeOfType<PaymentStatusChangedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OldStatus = PaymentStatus.CodPending,
                NewStatus = PaymentStatus.CodCollected
            });
        transaction.DomainEvents.Should().ContainSingle(e => e is CodCollectedEvent)
            .Which.Should().BeOfType<CodCollectedEvent>()
            .Which.CollectorName.Should().Be("Driver Pham");
    }

    [Fact]
    public void ConfirmCodCollection_WithNonCodMethod_ShouldThrow()
    {
        // Arrange
        var transaction = CreateTestTransaction(paymentMethod: PaymentMethod.EWallet);

        // Act
        var act = () => transaction.ConfirmCodCollection("Driver");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only COD payments can be confirmed for collection");
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
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Only COD payments can be confirmed for collection");
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
        transaction.GatewayResponseJson.Should().Be("{\"vnp_ResponseCode\":\"00\",\"vnp_Amount\":\"50000000\"}");
    }

    [Fact]
    public void SetGatewayFee_ShouldSetFeeAndCalculateNetAmount()
    {
        // Arrange
        var transaction = CreateTestTransaction(amount: 1_000_000m);

        // Act
        transaction.SetGatewayFee(15_000m);

        // Assert
        transaction.GatewayFee.Should().Be(15_000m);
        transaction.NetAmount.Should().Be(985_000m);
    }

    [Fact]
    public void SetGatewayFee_WithZeroFee_ShouldSetNetAmountEqualToAmount()
    {
        // Arrange
        var transaction = CreateTestTransaction(amount: 500_000m);

        // Act
        transaction.SetGatewayFee(0m);

        // Assert
        transaction.GatewayFee.Should().Be(0m);
        transaction.NetAmount.Should().Be(500_000m);
    }

    [Fact]
    public void SetGatewayTransactionId_ShouldSetValue()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.SetGatewayTransactionId("GW-EXTERNAL-ID-999");

        // Assert
        transaction.GatewayTransactionId.Should().Be("GW-EXTERNAL-ID-999");
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
        transaction.GatewayTransactionId.Should().Be("second-id");
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
        transaction.MetadataJson.Should().Be("{\"source\":\"checkout\",\"campaign\":\"summer2026\"}");
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
        transaction.MetadataJson.Should().Be("{\"v\":2}");
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
        transaction.DomainEvents.Should().HaveCount(4);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var transaction = CreateTestTransaction();
        transaction.MarkAsProcessing();
        transaction.DomainEvents.Should().HaveCountGreaterThan(0);

        // Act
        transaction.ClearDomainEvents();

        // Assert
        transaction.DomainEvents.Should().BeEmpty();
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_PendingToProcessingToPaid_ShouldTransitionCorrectly()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act & Assert
        transaction.Status.Should().Be(PaymentStatus.Pending);

        transaction.MarkAsProcessing();
        transaction.Status.Should().Be(PaymentStatus.Processing);

        transaction.MarkAsPaid("GW-TXN-FINAL");
        transaction.Status.Should().Be(PaymentStatus.Paid);
        transaction.PaidAt.Should().NotBeNull();
        transaction.GatewayTransactionId.Should().Be("GW-TXN-FINAL");
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
        transaction.Status.Should().Be(PaymentStatus.Failed);
        transaction.FailureReason.Should().Be("Gateway timeout");
        transaction.FailureCode.Should().Be("GW_TIMEOUT");
    }

    [Fact]
    public void FullLifecycle_PendingToCancelledToExpired_StatusShouldReflectLatest()
    {
        // Arrange
        var transaction = CreateTestTransaction();

        // Act
        transaction.MarkAsCancelled();

        // Assert
        transaction.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public void FullLifecycle_CodPayment_ShouldFollowCodPath()
    {
        // Arrange
        var transaction = CreateCodTransaction();

        // Act & Assert
        transaction.Status.Should().Be(PaymentStatus.Pending);
        transaction.PaymentMethod.Should().Be(PaymentMethod.COD);

        transaction.MarkAsCodPending();
        transaction.Status.Should().Be(PaymentStatus.CodPending);

        transaction.ConfirmCodCollection("Delivery Driver A");
        transaction.Status.Should().Be(PaymentStatus.CodCollected);
        transaction.CodCollectorName.Should().Be("Delivery Driver A");
        transaction.CodCollectedAt.Should().NotBeNull();
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
        transaction.Status.Should().Be(PaymentStatus.Refunded);
        transaction.PaidAt.Should().NotBeNull();
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
        transaction.OrderId.Should().Be(orderId);
        transaction.CustomerId.Should().Be(customerId);
        transaction.IpAddress.Should().Be("10.0.0.1");
        transaction.UserAgent.Should().Be("Chrome/120");
        transaction.ReturnUrl.Should().Be("https://shop.vn/return");
        transaction.ExpiresAt.Should().NotBeNull();
        transaction.Status.Should().Be(PaymentStatus.Paid);
        transaction.GatewayTransactionId.Should().Be("GW-TXN-FULL");
        transaction.GatewayFee.Should().Be(30_000m);
        transaction.NetAmount.Should().Be(1_970_000m);
        transaction.GatewayResponseJson.Should().Be("{\"code\":\"00\"}");
        transaction.MetadataJson.Should().Be("{\"checkoutSessionId\":\"sess-123\"}");
    }

    #endregion
}
