using NOIR.Domain.Entities.Payment;
using NOIR.Domain.Events.Payment;

namespace NOIR.Domain.UnitTests.Entities.Payment;

/// <summary>
/// Unit tests for the Refund aggregate root entity.
/// Tests factory method, approval workflow, processing states,
/// domain events, and status transitions.
/// </summary>
public class RefundTests
{
    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestTransactionId = Guid.NewGuid();

    #region Helper Methods

    private static Refund CreateTestRefund(
        string refundNumber = "REF-20260219-0001",
        Guid? paymentTransactionId = null,
        decimal amount = 250_000m,
        string currency = "VND",
        RefundReason reason = RefundReason.CustomerRequest,
        string? reasonDetail = "Customer changed mind",
        string requestedBy = "user-123",
        string? tenantId = TestTenantId)
    {
        return Refund.Create(
            refundNumber,
            paymentTransactionId ?? TestTransactionId,
            amount,
            currency,
            reason,
            reasonDetail,
            requestedBy,
            tenantId);
    }

    #endregion

    #region Create Factory Tests

    [Fact]
    public void Create_WithAllParameters_ShouldSetAllProperties()
    {
        // Arrange
        var transactionId = Guid.NewGuid();

        // Act
        var refund = Refund.Create(
            "REF-001", transactionId, 500_000m, "VND",
            RefundReason.Defective, "Product was broken",
            "admin-user", TestTenantId);

        // Assert
        refund.ShouldNotBeNull();
        refund.Id.ShouldNotBe(Guid.Empty);
        refund.RefundNumber.ShouldBe("REF-001");
        refund.PaymentTransactionId.ShouldBe(transactionId);
        refund.Amount.ShouldBe(500_000m);
        refund.Currency.ShouldBe("VND");
        refund.Reason.ShouldBe(RefundReason.Defective);
        refund.ReasonDetail.ShouldBe("Product was broken");
        refund.RequestedBy.ShouldBe("admin-user");
        refund.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var refund = CreateTestRefund();

        // Assert
        refund.Status.ShouldBe(RefundStatus.Pending);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var refund = CreateTestRefund();

        // Assert
        refund.GatewayRefundId.ShouldBeNull();
        refund.ApprovedBy.ShouldBeNull();
        refund.ProcessedAt.ShouldBeNull();
        refund.GatewayResponseJson.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullReasonDetail_ShouldAllowNull()
    {
        // Act
        var refund = CreateTestRefund(reasonDetail: null);

        // Assert
        refund.ReasonDetail.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var refund = CreateTestRefund(tenantId: null);

        // Assert
        refund.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldRaiseRefundRequestedEvent()
    {
        // Act
        var refund = CreateTestRefund(amount: 300_000m, reason: RefundReason.WrongItem);

        // Assert
        var __evt = refund.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<RefundRequestedEvent>();

        __evt.RefundId.ShouldBe(refund.Id);

        __evt.TransactionId.ShouldBe(TestTransactionId);

        __evt.Amount.ShouldBe(300_000m);

        __evt.Reason.ShouldBe(RefundReason.WrongItem);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var refund1 = CreateTestRefund(refundNumber: "REF-001");
        var refund2 = CreateTestRefund(refundNumber: "REF-002");

        // Assert
        refund1.Id.ShouldNotBe(refund2.Id);
    }

    #endregion

    #region Refund Reason Tests

    [Theory]
    [InlineData(RefundReason.CustomerRequest)]
    [InlineData(RefundReason.Defective)]
    [InlineData(RefundReason.WrongItem)]
    [InlineData(RefundReason.NotDelivered)]
    [InlineData(RefundReason.Duplicate)]
    [InlineData(RefundReason.Other)]
    public void Create_WithAllReasonTypes_ShouldSetCorrectReason(RefundReason reason)
    {
        // Act
        var refund = CreateTestRefund(reason: reason);

        // Assert
        refund.Reason.ShouldBe(reason);
    }

    #endregion

    #region Approve Tests

    [Fact]
    public void Approve_ShouldSetStatusToApproved()
    {
        // Arrange
        var refund = CreateTestRefund();

        // Act
        refund.Approve("admin-approver");

        // Assert
        refund.Status.ShouldBe(RefundStatus.Approved);
    }

    [Fact]
    public void Approve_ShouldSetApprovedBy()
    {
        // Arrange
        var refund = CreateTestRefund();

        // Act
        refund.Approve("admin-approver-123");

        // Assert
        refund.ApprovedBy.ShouldBe("admin-approver-123");
    }

    [Fact]
    public void Approve_ShouldNotAffectOtherProperties()
    {
        // Arrange
        var refund = CreateTestRefund();
        var originalAmount = refund.Amount;
        var originalReason = refund.Reason;

        // Act
        refund.Approve("admin");

        // Assert
        refund.Amount.ShouldBe(originalAmount);
        refund.Reason.ShouldBe(originalReason);
        refund.ProcessedAt.ShouldBeNull();
        refund.GatewayRefundId.ShouldBeNull();
    }

    #endregion

    #region MarkAsProcessing Tests

    [Fact]
    public void MarkAsProcessing_ShouldSetStatusToProcessing()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.Approve("admin");

        // Act
        refund.MarkAsProcessing();

        // Assert
        refund.Status.ShouldBe(RefundStatus.Processing);
    }

    #endregion

    #region Complete Tests

    [Fact]
    public void Complete_ShouldSetStatusToCompleted()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.Approve("admin");
        refund.MarkAsProcessing();

        // Act
        refund.Complete("gw-refund-12345");

        // Assert
        refund.Status.ShouldBe(RefundStatus.Completed);
    }

    [Fact]
    public void Complete_ShouldSetGatewayRefundId()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.Approve("admin");
        refund.MarkAsProcessing();

        // Act
        refund.Complete("gw-refund-67890");

        // Assert
        refund.GatewayRefundId.ShouldBe("gw-refund-67890");
    }

    [Fact]
    public void Complete_ShouldSetProcessedAt()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.Approve("admin");
        refund.MarkAsProcessing();
        var beforeComplete = DateTimeOffset.UtcNow;

        // Act
        refund.Complete("gw-refund-001");

        // Assert
        refund.ProcessedAt.ShouldNotBeNull();
        refund.ProcessedAt!.Value.ShouldBeGreaterThanOrEqualTo(beforeComplete);
    }

    [Fact]
    public void Complete_ShouldRaiseRefundCompletedEvent()
    {
        // Arrange
        var refund = CreateTestRefund(amount: 200_000m);
        refund.Approve("admin");
        refund.MarkAsProcessing();
        refund.ClearDomainEvents();

        // Act
        refund.Complete("gw-001");

        // Assert
        var __evt = refund.DomainEvents.ShouldHaveSingleItem()

            .ShouldBeOfType<RefundCompletedEvent>();

        __evt.RefundId.ShouldBe(refund.Id);

        __evt.TransactionId.ShouldBe(TestTransactionId);

        __evt.Amount.ShouldBe(200_000m);
    }

    #endregion

    #region Reject Tests

    [Fact]
    public void Reject_ShouldSetStatusToRejected()
    {
        // Arrange
        var refund = CreateTestRefund();

        // Act
        refund.Reject("Insufficient evidence");

        // Assert
        refund.Status.ShouldBe(RefundStatus.Rejected);
    }

    [Fact]
    public void Reject_ShouldSetReasonDetail()
    {
        // Arrange
        var refund = CreateTestRefund();

        // Act
        refund.Reject("Policy violation - item used beyond return window");

        // Assert
        refund.ReasonDetail.ShouldBe("Policy violation - item used beyond return window");
    }

    [Fact]
    public void Reject_ShouldOverwritePreviousReasonDetail()
    {
        // Arrange
        var refund = CreateTestRefund(reasonDetail: "Original reason");

        // Act
        refund.Reject("New rejection reason");

        // Assert
        refund.ReasonDetail.ShouldBe("New rejection reason");
    }

    #endregion

    #region MarkAsFailed Tests

    [Fact]
    public void MarkAsFailed_ShouldSetStatusToFailed()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.Approve("admin");
        refund.MarkAsProcessing();

        // Act
        refund.MarkAsFailed("{\"error\":\"Gateway timeout\"}");

        // Assert
        refund.Status.ShouldBe(RefundStatus.Failed);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetGatewayResponseJson()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.Approve("admin");
        refund.MarkAsProcessing();
        var responseJson = "{\"code\":\"TIMEOUT\",\"message\":\"Gateway request timed out\"}";

        // Act
        refund.MarkAsFailed(responseJson);

        // Assert
        refund.GatewayResponseJson.ShouldBe(responseJson);
    }

    #endregion

    #region Full Workflow Tests

    [Fact]
    public void FullWorkflow_PendingToCompleted_ShouldTransitionCorrectly()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.Status.ShouldBe(RefundStatus.Pending);

        // Act - Approve
        refund.Approve("admin-user");
        refund.Status.ShouldBe(RefundStatus.Approved);
        refund.ApprovedBy.ShouldBe("admin-user");

        // Act - Processing
        refund.MarkAsProcessing();
        refund.Status.ShouldBe(RefundStatus.Processing);

        // Act - Complete
        refund.Complete("gw-final-id");
        refund.Status.ShouldBe(RefundStatus.Completed);
        refund.GatewayRefundId.ShouldBe("gw-final-id");
        refund.ProcessedAt.ShouldNotBeNull();
    }

    [Fact]
    public void FullWorkflow_PendingToRejected_ShouldTransitionCorrectly()
    {
        // Arrange
        var refund = CreateTestRefund();

        // Act
        refund.Reject("Fraudulent request");

        // Assert
        refund.Status.ShouldBe(RefundStatus.Rejected);
        refund.ReasonDetail.ShouldBe("Fraudulent request");
    }

    [Fact]
    public void FullWorkflow_ProcessingToFailed_ShouldTransitionCorrectly()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.Approve("admin");
        refund.MarkAsProcessing();

        // Act
        refund.MarkAsFailed("{\"error\":\"Insufficient funds in merchant account\"}");

        // Assert
        refund.Status.ShouldBe(RefundStatus.Failed);
        refund.GatewayResponseJson.ShouldContain("Insufficient funds");
    }

    [Fact]
    public void DomainEvents_ShouldAccumulateAcrossWorkflow()
    {
        // Arrange
        var refund = CreateTestRefund();
        // RefundRequestedEvent already raised

        // Act
        refund.Approve("admin");
        refund.MarkAsProcessing();
        refund.Complete("gw-001");

        // Assert - RefundRequested + RefundCompleted
        refund.DomainEvents.Count().ShouldBe(2);
        refund.DomainEvents.ShouldContain(e => e is RefundRequestedEvent);
        refund.DomainEvents.ShouldContain(e => e is RefundCompletedEvent);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.DomainEvents.Count().ShouldBe(1);

        // Act
        refund.ClearDomainEvents();

        // Assert
        refund.DomainEvents.ShouldBeEmpty();
    }

    #endregion

    #region Currency Tests

    [Theory]
    [InlineData("VND")]
    [InlineData("USD")]
    [InlineData("EUR")]
    public void Create_WithDifferentCurrencies_ShouldSetCorrectly(string currency)
    {
        // Act
        var refund = CreateTestRefund(currency: currency);

        // Assert
        refund.Currency.ShouldBe(currency);
    }

    #endregion
}
