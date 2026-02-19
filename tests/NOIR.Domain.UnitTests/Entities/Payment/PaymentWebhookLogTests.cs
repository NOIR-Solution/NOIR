using NOIR.Domain.Entities.Payment;

namespace NOIR.Domain.UnitTests.Entities.Payment;

/// <summary>
/// Unit tests for the PaymentWebhookLog aggregate root entity.
/// Tests factory methods, request details, signature validation, processing status transitions,
/// retry tracking, and full webhook processing workflow.
/// </summary>
public class PaymentWebhookLogTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestProvider = "vnpay";
    private const string TestEventType = "payment.success";
    private const string TestRequestBody = "{\"vnp_ResponseCode\":\"00\",\"vnp_Amount\":\"50000000\"}";
    private static readonly Guid TestGatewayId = Guid.NewGuid();

    /// <summary>
    /// Helper to create a default valid PaymentWebhookLog for tests.
    /// </summary>
    private static PaymentWebhookLog CreateTestWebhookLog(
        Guid? paymentGatewayId = null,
        string provider = TestProvider,
        string eventType = TestEventType,
        string requestBody = TestRequestBody,
        string? tenantId = TestTenantId)
    {
        return PaymentWebhookLog.Create(
            paymentGatewayId ?? TestGatewayId,
            provider,
            eventType,
            requestBody,
            tenantId);
    }

    #region Create Factory

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidLog()
    {
        // Act
        var log = PaymentWebhookLog.Create(
            TestGatewayId, TestProvider, TestEventType, TestRequestBody, TestTenantId);

        // Assert
        log.Should().NotBeNull();
        log.Id.Should().NotBe(Guid.Empty);
        log.PaymentGatewayId.Should().Be(TestGatewayId);
        log.Provider.Should().Be(TestProvider);
        log.EventType.Should().Be(TestEventType);
        log.RequestBody.Should().Be(TestRequestBody);
        log.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetProcessingStatusToReceived()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Received);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.GatewayEventId.Should().BeNull();
        log.RequestHeaders.Should().BeNull();
        log.SignatureValue.Should().BeNull();
        log.ProcessingError.Should().BeNull();
        log.PaymentTransactionId.Should().BeNull();
        log.IpAddress.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldInitializeDefaultValues()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.SignatureValid.Should().BeFalse();
        log.RetryCount.Should().Be(0);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var log = CreateTestWebhookLog(tenantId: null);

        // Assert
        log.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_MultipleInstances_ShouldHaveUniqueIds()
    {
        // Act
        var log1 = CreateTestWebhookLog(eventType: "payment.success");
        var log2 = CreateTestWebhookLog(eventType: "payment.failed");

        // Assert
        log1.Id.Should().NotBe(log2.Id);
    }

    [Fact]
    public void Create_WithDifferentProviders_ShouldSetCorrectly()
    {
        // Act
        var momoLog = CreateTestWebhookLog(provider: "momo");
        var zalopayLog = CreateTestWebhookLog(provider: "zalopay");

        // Assert
        momoLog.Provider.Should().Be("momo");
        zalopayLog.Provider.Should().Be("zalopay");
    }

    [Theory]
    [InlineData("payment.success")]
    [InlineData("payment.failed")]
    [InlineData("refund.completed")]
    [InlineData("refund.failed")]
    [InlineData("chargeback.created")]
    public void Create_WithVariousEventTypes_ShouldSetCorrectly(string eventType)
    {
        // Act
        var log = CreateTestWebhookLog(eventType: eventType);

        // Assert
        log.EventType.Should().Be(eventType);
    }

    #endregion

    #region SetRequestDetails

    [Fact]
    public void SetRequestDetails_ShouldSetAllFields()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.SetRequestDetails(
            "{\"Content-Type\":\"application/json\",\"X-Signature\":\"abc123\"}",
            "abc123",
            "203.0.113.50");

        // Assert
        log.RequestHeaders.Should().Be("{\"Content-Type\":\"application/json\",\"X-Signature\":\"abc123\"}");
        log.SignatureValue.Should().Be("abc123");
        log.IpAddress.Should().Be("203.0.113.50");
    }

    [Fact]
    public void SetRequestDetails_WithNullValues_ShouldAllowNulls()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.SetRequestDetails(null, null, null);

        // Assert
        log.RequestHeaders.Should().BeNull();
        log.SignatureValue.Should().BeNull();
        log.IpAddress.Should().BeNull();
    }

    [Fact]
    public void SetRequestDetails_CalledTwice_ShouldOverwritePreviousValues()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.SetRequestDetails("{\"old\":true}", "old-sig", "1.1.1.1");

        // Act
        log.SetRequestDetails("{\"new\":true}", "new-sig", "2.2.2.2");

        // Assert
        log.RequestHeaders.Should().Be("{\"new\":true}");
        log.SignatureValue.Should().Be("new-sig");
        log.IpAddress.Should().Be("2.2.2.2");
    }

    #endregion

    #region SetGatewayEventId

    [Fact]
    public void SetGatewayEventId_ShouldSetValue()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.SetGatewayEventId("evt_vnpay_abc123");

        // Assert
        log.GatewayEventId.Should().Be("evt_vnpay_abc123");
    }

    [Fact]
    public void SetGatewayEventId_CalledTwice_ShouldOverwritePreviousValue()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.SetGatewayEventId("first-event-id");

        // Act
        log.SetGatewayEventId("second-event-id");

        // Assert
        log.GatewayEventId.Should().Be("second-event-id");
    }

    #endregion

    #region MarkSignatureValid

    [Fact]
    public void MarkSignatureValid_WithTrue_ShouldSetSignatureValidTrue()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkSignatureValid(true);

        // Assert
        log.SignatureValid.Should().BeTrue();
    }

    [Fact]
    public void MarkSignatureValid_WithFalse_ShouldSetSignatureValidFalse()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkSignatureValid(false);

        // Assert
        log.SignatureValid.Should().BeFalse();
    }

    [Fact]
    public void MarkSignatureValid_CalledMultipleTimes_ShouldReflectLatestValue()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkSignatureValid(true);
        log.MarkSignatureValid(false);

        // Assert
        log.SignatureValid.Should().BeFalse();
    }

    #endregion

    #region Processing Status Transitions

    [Fact]
    public void MarkAsProcessing_ShouldTransitionToProcessing()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsProcessing();

        // Assert
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Processing);
    }

    [Fact]
    public void MarkAsProcessed_ShouldTransitionToProcessed()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.MarkAsProcessing();

        // Act
        log.MarkAsProcessed();

        // Assert
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Processed);
    }

    [Fact]
    public void MarkAsProcessed_WithTransactionId_ShouldSetPaymentTransactionId()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.MarkAsProcessing();
        var transactionId = Guid.NewGuid();

        // Act
        log.MarkAsProcessed(transactionId);

        // Assert
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Processed);
        log.PaymentTransactionId.Should().Be(transactionId);
    }

    [Fact]
    public void MarkAsProcessed_WithoutTransactionId_ShouldLeaveTransactionIdNull()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.MarkAsProcessing();

        // Act
        log.MarkAsProcessed();

        // Assert
        log.PaymentTransactionId.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_ShouldTransitionToFailed()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.MarkAsProcessing();

        // Act
        log.MarkAsFailed("Invalid signature");

        // Assert
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Failed);
    }

    [Fact]
    public void MarkAsFailed_ShouldSetProcessingError()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.MarkAsProcessing();

        // Act
        log.MarkAsFailed("Transaction not found");

        // Assert
        log.ProcessingError.Should().Be("Transaction not found");
    }

    [Fact]
    public void MarkAsFailed_ShouldIncrementRetryCount()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.RetryCount.Should().Be(0);

        // Act
        log.MarkAsFailed("First failure");

        // Assert
        log.RetryCount.Should().Be(1);
    }

    [Fact]
    public void MarkAsFailed_CalledMultipleTimes_ShouldIncrementRetryCountEachTime()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsFailed("Failure 1");
        log.MarkAsFailed("Failure 2");
        log.MarkAsFailed("Failure 3");

        // Assert
        log.RetryCount.Should().Be(3);
        log.ProcessingError.Should().Be("Failure 3");
    }

    [Fact]
    public void MarkAsFailed_ShouldOverwritePreviousProcessingError()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.MarkAsFailed("First error");

        // Act
        log.MarkAsFailed("Second error");

        // Assert
        log.ProcessingError.Should().Be("Second error");
    }

    [Fact]
    public void MarkAsSkipped_ShouldTransitionToSkipped()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsSkipped("Duplicate event");

        // Assert
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Skipped);
    }

    [Fact]
    public void MarkAsSkipped_ShouldSetProcessingError()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsSkipped("Event already processed");

        // Assert
        log.ProcessingError.Should().Be("Event already processed");
    }

    [Fact]
    public void MarkAsSkipped_ShouldNotIncrementRetryCount()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsSkipped("Irrelevant event type");

        // Assert
        log.RetryCount.Should().Be(0);
    }

    #endregion

    #region Full Workflow

    [Fact]
    public void FullWorkflow_SuccessfulProcessing_ShouldSetAllFieldsCorrectly()
    {
        // Arrange
        var gatewayId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        // Act
        var log = PaymentWebhookLog.Create(
            gatewayId, "vnpay", "payment.success",
            "{\"vnp_ResponseCode\":\"00\"}", TestTenantId);

        log.SetRequestDetails(
            "{\"Content-Type\":\"application/json\"}",
            "sig-abc123",
            "203.0.113.100");
        log.SetGatewayEventId("evt_vnpay_001");
        log.MarkSignatureValid(true);
        log.MarkAsProcessing();
        log.MarkAsProcessed(transactionId);

        // Assert
        log.PaymentGatewayId.Should().Be(gatewayId);
        log.Provider.Should().Be("vnpay");
        log.EventType.Should().Be("payment.success");
        log.RequestBody.Should().Be("{\"vnp_ResponseCode\":\"00\"}");
        log.RequestHeaders.Should().Be("{\"Content-Type\":\"application/json\"}");
        log.SignatureValue.Should().Be("sig-abc123");
        log.IpAddress.Should().Be("203.0.113.100");
        log.GatewayEventId.Should().Be("evt_vnpay_001");
        log.SignatureValid.Should().BeTrue();
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Processed);
        log.PaymentTransactionId.Should().Be(transactionId);
        log.ProcessingError.Should().BeNull();
        log.RetryCount.Should().Be(0);
    }

    [Fact]
    public void FullWorkflow_FailedThenRetried_ShouldTrackAttempts()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act - first attempt fails
        log.MarkSignatureValid(true);
        log.MarkAsProcessing();
        log.MarkAsFailed("Database connection error");

        // Assert first failure
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Failed);
        log.RetryCount.Should().Be(1);
        log.ProcessingError.Should().Be("Database connection error");

        // Act - retry and fail again
        log.MarkAsProcessing();
        log.MarkAsFailed("Deadlock detected");

        // Assert second failure
        log.RetryCount.Should().Be(2);
        log.ProcessingError.Should().Be("Deadlock detected");

        // Act - retry and succeed
        log.MarkAsProcessing();
        var transactionId = Guid.NewGuid();
        log.MarkAsProcessed(transactionId);

        // Assert final success
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Processed);
        log.PaymentTransactionId.Should().Be(transactionId);
        log.RetryCount.Should().Be(2);
    }

    [Fact]
    public void FullWorkflow_InvalidSignature_ShouldSkip()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.SetRequestDetails("{\"headers\":true}", "invalid-sig", "10.0.0.1");
        log.MarkSignatureValid(false);
        log.MarkAsSkipped("Invalid signature - possible forgery");

        // Assert
        log.SignatureValid.Should().BeFalse();
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Skipped);
        log.ProcessingError.Should().Be("Invalid signature - possible forgery");
        log.RetryCount.Should().Be(0);
    }

    [Fact]
    public void FullWorkflow_DuplicateEvent_ShouldSkip()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.SetGatewayEventId("evt_already_processed");
        log.MarkSignatureValid(true);
        log.MarkAsSkipped("Duplicate event: evt_already_processed");

        // Assert
        log.ProcessingStatus.Should().Be(WebhookProcessingStatus.Skipped);
        log.ProcessingError.Should().Contain("Duplicate event");
    }

    #endregion
}
