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
        log.ShouldNotBeNull();
        log.Id.ShouldNotBe(Guid.Empty);
        log.PaymentGatewayId.ShouldBe(TestGatewayId);
        log.Provider.ShouldBe(TestProvider);
        log.EventType.ShouldBe(TestEventType);
        log.RequestBody.ShouldBe(TestRequestBody);
        log.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldSetProcessingStatusToReceived()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Received);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.GatewayEventId.ShouldBeNull();
        log.RequestHeaders.ShouldBeNull();
        log.SignatureValue.ShouldBeNull();
        log.ProcessingError.ShouldBeNull();
        log.PaymentTransactionId.ShouldBeNull();
        log.IpAddress.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldInitializeDefaultValues()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.SignatureValid.ShouldBeFalse();
        log.RetryCount.ShouldBe(0);
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var log = CreateTestWebhookLog(tenantId: null);

        // Assert
        log.TenantId.ShouldBeNull();
    }

    [Fact]
    public void Create_MultipleInstances_ShouldHaveUniqueIds()
    {
        // Act
        var log1 = CreateTestWebhookLog(eventType: "payment.success");
        var log2 = CreateTestWebhookLog(eventType: "payment.failed");

        // Assert
        log1.Id.ShouldNotBe(log2.Id);
    }

    [Fact]
    public void Create_WithDifferentProviders_ShouldSetCorrectly()
    {
        // Act
        var momoLog = CreateTestWebhookLog(provider: "momo");
        var zalopayLog = CreateTestWebhookLog(provider: "zalopay");

        // Assert
        momoLog.Provider.ShouldBe("momo");
        zalopayLog.Provider.ShouldBe("zalopay");
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
        log.EventType.ShouldBe(eventType);
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
        log.RequestHeaders.ShouldBe("{\"Content-Type\":\"application/json\",\"X-Signature\":\"abc123\"}");
        log.SignatureValue.ShouldBe("abc123");
        log.IpAddress.ShouldBe("203.0.113.50");
    }

    [Fact]
    public void SetRequestDetails_WithNullValues_ShouldAllowNulls()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.SetRequestDetails(null, null, null);

        // Assert
        log.RequestHeaders.ShouldBeNull();
        log.SignatureValue.ShouldBeNull();
        log.IpAddress.ShouldBeNull();
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
        log.RequestHeaders.ShouldBe("{\"new\":true}");
        log.SignatureValue.ShouldBe("new-sig");
        log.IpAddress.ShouldBe("2.2.2.2");
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
        log.GatewayEventId.ShouldBe("evt_vnpay_abc123");
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
        log.GatewayEventId.ShouldBe("second-event-id");
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
        log.SignatureValid.ShouldBeTrue();
    }

    [Fact]
    public void MarkSignatureValid_WithFalse_ShouldSetSignatureValidFalse()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkSignatureValid(false);

        // Assert
        log.SignatureValid.ShouldBeFalse();
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
        log.SignatureValid.ShouldBeFalse();
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
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Processing);
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
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Processed);
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
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Processed);
        log.PaymentTransactionId.ShouldBe(transactionId);
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
        log.PaymentTransactionId.ShouldBeNull();
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
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Failed);
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
        log.ProcessingError.ShouldBe("Transaction not found");
    }

    [Fact]
    public void MarkAsFailed_ShouldIncrementRetryCount()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.RetryCount.ShouldBe(0);

        // Act
        log.MarkAsFailed("First failure");

        // Assert
        log.RetryCount.ShouldBe(1);
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
        log.RetryCount.ShouldBe(3);
        log.ProcessingError.ShouldBe("Failure 3");
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
        log.ProcessingError.ShouldBe("Second error");
    }

    [Fact]
    public void MarkAsSkipped_ShouldTransitionToSkipped()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsSkipped("Duplicate event");

        // Assert
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Skipped);
    }

    [Fact]
    public void MarkAsSkipped_ShouldSetProcessingError()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsSkipped("Event already processed");

        // Assert
        log.ProcessingError.ShouldBe("Event already processed");
    }

    [Fact]
    public void MarkAsSkipped_ShouldNotIncrementRetryCount()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsSkipped("Irrelevant event type");

        // Assert
        log.RetryCount.ShouldBe(0);
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
        log.PaymentGatewayId.ShouldBe(gatewayId);
        log.Provider.ShouldBe("vnpay");
        log.EventType.ShouldBe("payment.success");
        log.RequestBody.ShouldBe("{\"vnp_ResponseCode\":\"00\"}");
        log.RequestHeaders.ShouldBe("{\"Content-Type\":\"application/json\"}");
        log.SignatureValue.ShouldBe("sig-abc123");
        log.IpAddress.ShouldBe("203.0.113.100");
        log.GatewayEventId.ShouldBe("evt_vnpay_001");
        log.SignatureValid.ShouldBeTrue();
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Processed);
        log.PaymentTransactionId.ShouldBe(transactionId);
        log.ProcessingError.ShouldBeNull();
        log.RetryCount.ShouldBe(0);
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
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Failed);
        log.RetryCount.ShouldBe(1);
        log.ProcessingError.ShouldBe("Database connection error");

        // Act - retry and fail again
        log.MarkAsProcessing();
        log.MarkAsFailed("Deadlock detected");

        // Assert second failure
        log.RetryCount.ShouldBe(2);
        log.ProcessingError.ShouldBe("Deadlock detected");

        // Act - retry and succeed
        log.MarkAsProcessing();
        var transactionId = Guid.NewGuid();
        log.MarkAsProcessed(transactionId);

        // Assert final success
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Processed);
        log.PaymentTransactionId.ShouldBe(transactionId);
        log.RetryCount.ShouldBe(2);
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
        log.SignatureValid.ShouldBeFalse();
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Skipped);
        log.ProcessingError.ShouldBe("Invalid signature - possible forgery");
        log.RetryCount.ShouldBe(0);
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
        log.ProcessingStatus.ShouldBe(WebhookProcessingStatus.Skipped);
        log.ProcessingError.ShouldContain("Duplicate event");
    }

    #endregion
}
