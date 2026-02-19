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
        refund.Should().NotBeNull();
        refund.Id.Should().NotBe(Guid.Empty);
        refund.RefundNumber.Should().Be("REF-001");
        refund.PaymentTransactionId.Should().Be(transactionId);
        refund.Amount.Should().Be(500_000m);
        refund.Currency.Should().Be("VND");
        refund.Reason.Should().Be(RefundReason.Defective);
        refund.ReasonDetail.Should().Be("Product was broken");
        refund.RequestedBy.Should().Be("admin-user");
        refund.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var refund = CreateTestRefund();

        // Assert
        refund.Status.Should().Be(RefundStatus.Pending);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var refund = CreateTestRefund();

        // Assert
        refund.GatewayRefundId.Should().BeNull();
        refund.ApprovedBy.Should().BeNull();
        refund.ProcessedAt.Should().BeNull();
        refund.GatewayResponseJson.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullReasonDetail_ShouldAllowNull()
    {
        // Act
        var refund = CreateTestRefund(reasonDetail: null);

        // Assert
        refund.ReasonDetail.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var refund = CreateTestRefund(tenantId: null);

        // Assert
        refund.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldRaiseRefundRequestedEvent()
    {
        // Act
        var refund = CreateTestRefund(amount: 300_000m, reason: RefundReason.WrongItem);

        // Assert
        refund.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RefundRequestedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                RefundId = refund.Id,
                TransactionId = TestTransactionId,
                Amount = 300_000m,
                Reason = RefundReason.WrongItem
            });
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Act
        var refund1 = CreateTestRefund(refundNumber: "REF-001");
        var refund2 = CreateTestRefund(refundNumber: "REF-002");

        // Assert
        refund1.Id.Should().NotBe(refund2.Id);
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
        refund.Reason.Should().Be(reason);
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
        refund.Status.Should().Be(RefundStatus.Approved);
    }

    [Fact]
    public void Approve_ShouldSetApprovedBy()
    {
        // Arrange
        var refund = CreateTestRefund();

        // Act
        refund.Approve("admin-approver-123");

        // Assert
        refund.ApprovedBy.Should().Be("admin-approver-123");
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
        refund.Amount.Should().Be(originalAmount);
        refund.Reason.Should().Be(originalReason);
        refund.ProcessedAt.Should().BeNull();
        refund.GatewayRefundId.Should().BeNull();
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
        refund.Status.Should().Be(RefundStatus.Processing);
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
        refund.Status.Should().Be(RefundStatus.Completed);
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
        refund.GatewayRefundId.Should().Be("gw-refund-67890");
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
        refund.ProcessedAt.Should().NotBeNull();
        refund.ProcessedAt.Should().BeOnOrAfter(beforeComplete);
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
        refund.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<RefundCompletedEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                RefundId = refund.Id,
                TransactionId = TestTransactionId,
                Amount = 200_000m
            });
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
        refund.Status.Should().Be(RefundStatus.Rejected);
    }

    [Fact]
    public void Reject_ShouldSetReasonDetail()
    {
        // Arrange
        var refund = CreateTestRefund();

        // Act
        refund.Reject("Policy violation - item used beyond return window");

        // Assert
        refund.ReasonDetail.Should().Be("Policy violation - item used beyond return window");
    }

    [Fact]
    public void Reject_ShouldOverwritePreviousReasonDetail()
    {
        // Arrange
        var refund = CreateTestRefund(reasonDetail: "Original reason");

        // Act
        refund.Reject("New rejection reason");

        // Assert
        refund.ReasonDetail.Should().Be("New rejection reason");
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
        refund.Status.Should().Be(RefundStatus.Failed);
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
        refund.GatewayResponseJson.Should().Be(responseJson);
    }

    #endregion

    #region Full Workflow Tests

    [Fact]
    public void FullWorkflow_PendingToCompleted_ShouldTransitionCorrectly()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.Status.Should().Be(RefundStatus.Pending);

        // Act - Approve
        refund.Approve("admin-user");
        refund.Status.Should().Be(RefundStatus.Approved);
        refund.ApprovedBy.Should().Be("admin-user");

        // Act - Processing
        refund.MarkAsProcessing();
        refund.Status.Should().Be(RefundStatus.Processing);

        // Act - Complete
        refund.Complete("gw-final-id");
        refund.Status.Should().Be(RefundStatus.Completed);
        refund.GatewayRefundId.Should().Be("gw-final-id");
        refund.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void FullWorkflow_PendingToRejected_ShouldTransitionCorrectly()
    {
        // Arrange
        var refund = CreateTestRefund();

        // Act
        refund.Reject("Fraudulent request");

        // Assert
        refund.Status.Should().Be(RefundStatus.Rejected);
        refund.ReasonDetail.Should().Be("Fraudulent request");
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
        refund.Status.Should().Be(RefundStatus.Failed);
        refund.GatewayResponseJson.Should().Contain("Insufficient funds");
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
        refund.DomainEvents.Should().HaveCount(2);
        refund.DomainEvents.Should().ContainSingle(e => e is RefundRequestedEvent);
        refund.DomainEvents.Should().ContainSingle(e => e is RefundCompletedEvent);
    }

    [Fact]
    public void ClearDomainEvents_ShouldRemoveAllEvents()
    {
        // Arrange
        var refund = CreateTestRefund();
        refund.DomainEvents.Should().HaveCount(1);

        // Act
        refund.ClearDomainEvents();

        // Assert
        refund.DomainEvents.Should().BeEmpty();
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
        refund.Currency.Should().Be(currency);
    }

    #endregion
}
