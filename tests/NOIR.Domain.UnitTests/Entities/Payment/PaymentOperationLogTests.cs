using NOIR.Domain.Entities.Payment;

namespace NOIR.Domain.UnitTests.Entities.Payment;

/// <summary>
/// Unit tests for the PaymentOperationLog aggregate root entity.
/// Tests factory methods, transaction info, refund info, request/response data truncation,
/// duration, success/failure marking, additional context, and user info.
/// </summary>
public class PaymentOperationLogTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestProvider = "vnpay";
    private const string TestCorrelationId = "corr-123-abc";

    /// <summary>
    /// Helper to create a default valid PaymentOperationLog for tests.
    /// </summary>
    private static PaymentOperationLog CreateTestLog(
        PaymentOperationType operationType = PaymentOperationType.InitiatePayment,
        string provider = TestProvider,
        string correlationId = TestCorrelationId,
        string? tenantId = TestTenantId)
    {
        return PaymentOperationLog.Create(operationType, provider, correlationId, tenantId);
    }

    #region Create Factory

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidLog()
    {
        // Act
        var log = PaymentOperationLog.Create(
            PaymentOperationType.InitiatePayment, TestProvider, TestCorrelationId, TestTenantId);

        // Assert
        log.ShouldNotBeNull();
        log.Id.ShouldNotBe(Guid.Empty);
        log.OperationType.ShouldBe(PaymentOperationType.InitiatePayment);
        log.Provider.ShouldBe(TestProvider);
        log.CorrelationId.ShouldBe(TestCorrelationId);
        log.TenantId.ShouldBe(TestTenantId);
    }

    [Fact]
    public void Create_ShouldInitializeNullablePropertiesToNull()
    {
        // Act
        var log = CreateTestLog();

        // Assert
        log.PaymentTransactionId.ShouldBeNull();
        log.TransactionNumber.ShouldBeNull();
        log.RefundId.ShouldBeNull();
        log.RequestData.ShouldBeNull();
        log.ResponseData.ShouldBeNull();
        log.HttpStatusCode.ShouldBeNull();
        log.ErrorCode.ShouldBeNull();
        log.ErrorMessage.ShouldBeNull();
        log.StackTrace.ShouldBeNull();
        log.AdditionalContext.ShouldBeNull();
        log.UserId.ShouldBeNull();
        log.IpAddress.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldInitializeDefaultValues()
    {
        // Act
        var log = CreateTestLog();

        // Assert
        log.DurationMs.ShouldBe(0);
        log.Success.ShouldBeFalse();
    }

    [Fact]
    public void Create_WithNullTenantId_ShouldAllowNull()
    {
        // Act
        var log = CreateTestLog(tenantId: null);

        // Assert
        log.TenantId.ShouldBeNull();
    }

    [Theory]
    [InlineData(PaymentOperationType.InitiatePayment)]
    [InlineData(PaymentOperationType.GetPaymentStatus)]
    [InlineData(PaymentOperationType.ValidateWebhook)]
    [InlineData(PaymentOperationType.ProcessWebhook)]
    [InlineData(PaymentOperationType.InitiateRefund)]
    [InlineData(PaymentOperationType.TestConnection)]
    [InlineData(PaymentOperationType.HealthCheck)]
    public void Create_WithVariousOperationTypes_ShouldSetCorrectly(PaymentOperationType opType)
    {
        // Act
        var log = CreateTestLog(operationType: opType);

        // Assert
        log.OperationType.ShouldBe(opType);
    }

    [Fact]
    public void Create_MultipleInstances_ShouldHaveUniqueIds()
    {
        // Act
        var log1 = CreateTestLog(correlationId: "corr-1");
        var log2 = CreateTestLog(correlationId: "corr-2");

        // Assert
        log1.Id.ShouldNotBe(log2.Id);
    }

    #endregion

    #region SetTransactionInfo

    [Fact]
    public void SetTransactionInfo_ShouldSetTransactionIdAndNumber()
    {
        // Arrange
        var log = CreateTestLog();
        var transactionId = Guid.NewGuid();

        // Act
        log.SetTransactionInfo(transactionId, "TXN-20260219-0001");

        // Assert
        log.PaymentTransactionId.ShouldBe(transactionId);
        log.TransactionNumber.ShouldBe("TXN-20260219-0001");
    }

    [Fact]
    public void SetTransactionInfo_WithNullValues_ShouldAllowNulls()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetTransactionInfo(null, null);

        // Assert
        log.PaymentTransactionId.ShouldBeNull();
        log.TransactionNumber.ShouldBeNull();
    }

    [Fact]
    public void SetTransactionInfo_CalledTwice_ShouldOverwritePreviousValues()
    {
        // Arrange
        var log = CreateTestLog();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        log.SetTransactionInfo(firstId, "TXN-001");

        // Act
        log.SetTransactionInfo(secondId, "TXN-002");

        // Assert
        log.PaymentTransactionId.ShouldBe(secondId);
        log.TransactionNumber.ShouldBe("TXN-002");
    }

    #endregion

    #region SetRefundInfo

    [Fact]
    public void SetRefundInfo_ShouldSetRefundId()
    {
        // Arrange
        var log = CreateTestLog();
        var refundId = Guid.NewGuid();

        // Act
        log.SetRefundInfo(refundId);

        // Assert
        log.RefundId.ShouldBe(refundId);
    }

    [Fact]
    public void SetRefundInfo_CalledTwice_ShouldOverwritePreviousValue()
    {
        // Arrange
        var log = CreateTestLog();
        var firstRefundId = Guid.NewGuid();
        var secondRefundId = Guid.NewGuid();
        log.SetRefundInfo(firstRefundId);

        // Act
        log.SetRefundInfo(secondRefundId);

        // Assert
        log.RefundId.ShouldBe(secondRefundId);
    }

    #endregion

    #region SetRequestData

    [Fact]
    public void SetRequestData_WithShortData_ShouldSetAsIs()
    {
        // Arrange
        var log = CreateTestLog();
        var requestData = "{\"amount\":100000,\"currency\":\"VND\"}";

        // Act
        log.SetRequestData(requestData);

        // Assert
        log.RequestData.ShouldBe(requestData);
    }

    [Fact]
    public void SetRequestData_WithNull_ShouldSetNull()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetRequestData(null);

        // Assert
        log.RequestData.ShouldBeNull();
    }

    [Fact]
    public void SetRequestData_ExceedingMaxLength_ShouldTruncate()
    {
        // Arrange
        var log = CreateTestLog();
        var longData = new string('X', 11_000); // Over 10240 limit

        // Act
        log.SetRequestData(longData);

        // Assert
        log.RequestData.Length.ShouldBe(10240 + "...[TRUNCATED]".Length);
        log.RequestData.ShouldEndWith("...[TRUNCATED]");
    }

    [Fact]
    public void SetRequestData_ExactlyAtMaxLength_ShouldNotTruncate()
    {
        // Arrange
        var log = CreateTestLog();
        var exactData = new string('X', 10240);

        // Act
        log.SetRequestData(exactData);

        // Assert
        log.RequestData.Length.ShouldBe(10240);
        log.RequestData.ShouldNotContain("TRUNCATED");
    }

    [Fact]
    public void SetRequestData_OneOverMaxLength_ShouldTruncate()
    {
        // Arrange
        var log = CreateTestLog();
        var overData = new string('X', 10241);

        // Act
        log.SetRequestData(overData);

        // Assert
        log.RequestData.ShouldEndWith("...[TRUNCATED]");
    }

    #endregion

    #region SetResponseData

    [Fact]
    public void SetResponseData_WithShortData_ShouldSetAsIs()
    {
        // Arrange
        var log = CreateTestLog();
        var responseData = "{\"status\":\"success\",\"transactionId\":\"abc123\"}";

        // Act
        log.SetResponseData(responseData, 200);

        // Assert
        log.ResponseData.ShouldBe(responseData);
        log.HttpStatusCode.ShouldBe(200);
    }

    [Fact]
    public void SetResponseData_WithNull_ShouldSetNull()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetResponseData(null);

        // Assert
        log.ResponseData.ShouldBeNull();
        log.HttpStatusCode.ShouldBeNull();
    }

    [Fact]
    public void SetResponseData_ExceedingMaxLength_ShouldTruncate()
    {
        // Arrange
        var log = CreateTestLog();
        var longData = new string('Y', 11_000);

        // Act
        log.SetResponseData(longData, 500);

        // Assert
        log.ResponseData.ShouldEndWith("...[TRUNCATED]");
        log.HttpStatusCode.ShouldBe(500);
    }

    [Fact]
    public void SetResponseData_WithoutHttpStatusCode_ShouldDefaultToNull()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetResponseData("{\"ok\":true}");

        // Assert
        log.ResponseData.ShouldBe("{\"ok\":true}");
        log.HttpStatusCode.ShouldBeNull();
    }

    [Theory]
    [InlineData(200)]
    [InlineData(400)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(503)]
    public void SetResponseData_WithVariousHttpStatusCodes_ShouldSetCorrectly(int statusCode)
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetResponseData("response body", statusCode);

        // Assert
        log.HttpStatusCode.ShouldBe(statusCode);
    }

    #endregion

    #region SetDuration

    [Fact]
    public void SetDuration_ShouldSetDurationMs()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetDuration(1500);

        // Assert
        log.DurationMs.ShouldBe(1500);
    }

    [Fact]
    public void SetDuration_WithZero_ShouldSetZero()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetDuration(0);

        // Assert
        log.DurationMs.ShouldBe(0);
    }

    [Fact]
    public void SetDuration_CalledTwice_ShouldOverwritePreviousValue()
    {
        // Arrange
        var log = CreateTestLog();
        log.SetDuration(100);

        // Act
        log.SetDuration(250);

        // Assert
        log.DurationMs.ShouldBe(250);
    }

    #endregion

    #region MarkAsSuccess

    [Fact]
    public void MarkAsSuccess_ShouldSetSuccessToTrue()
    {
        // Arrange
        var log = CreateTestLog();
        log.Success.ShouldBeFalse();

        // Act
        log.MarkAsSuccess();

        // Assert
        log.Success.ShouldBeTrue();
    }

    #endregion

    #region MarkAsFailed

    [Fact]
    public void MarkAsFailed_ShouldSetSuccessToFalse()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.MarkAsFailed("ERR_001", "Payment declined", null);

        // Assert
        log.Success.ShouldBeFalse();
    }

    [Fact]
    public void MarkAsFailed_ShouldSetErrorCodeAndMessage()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.MarkAsFailed("ERR_TIMEOUT", "Connection timed out");

        // Assert
        log.ErrorCode.ShouldBe("ERR_TIMEOUT");
        log.ErrorMessage.ShouldBe("Connection timed out");
    }

    [Fact]
    public void MarkAsFailed_WithStackTrace_ShouldSetStackTrace()
    {
        // Arrange
        var log = CreateTestLog();
        var stackTrace = "at MyService.ProcessPayment() in Service.cs:line 42";

        // Act
        log.MarkAsFailed("ERR_001", "Internal error", stackTrace);

        // Assert
        log.StackTrace.ShouldBe(stackTrace);
    }

    [Fact]
    public void MarkAsFailed_WithNullParameters_ShouldAllowNulls()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.MarkAsFailed(null, null, null);

        // Assert
        log.ErrorCode.ShouldBeNull();
        log.ErrorMessage.ShouldBeNull();
        log.StackTrace.ShouldBeNull();
    }

    [Fact]
    public void MarkAsFailed_WithLongErrorMessage_ShouldTruncateAt2000()
    {
        // Arrange
        var log = CreateTestLog();
        var longMessage = new string('M', 2500);

        // Act
        log.MarkAsFailed("ERR_001", longMessage);

        // Assert
        log.ErrorMessage.Length.ShouldBe(2000 + "...[TRUNCATED]".Length);
        log.ErrorMessage.ShouldEndWith("...[TRUNCATED]");
    }

    [Fact]
    public void MarkAsFailed_WithErrorMessageExactly2000_ShouldNotTruncate()
    {
        // Arrange
        var log = CreateTestLog();
        var exactMessage = new string('M', 2000);

        // Act
        log.MarkAsFailed("ERR_001", exactMessage);

        // Assert
        log.ErrorMessage.Length.ShouldBe(2000);
        log.ErrorMessage.ShouldNotContain("TRUNCATED");
    }

    [Fact]
    public void MarkAsFailed_WithLongStackTrace_ShouldTruncateAt4000()
    {
        // Arrange
        var log = CreateTestLog();
        var longStackTrace = new string('S', 5000);

        // Act
        log.MarkAsFailed("ERR_001", "Error", longStackTrace);

        // Assert
        log.StackTrace.Length.ShouldBe(4000 + "...[TRUNCATED]".Length);
        log.StackTrace.ShouldEndWith("...[TRUNCATED]");
    }

    [Fact]
    public void MarkAsFailed_WithStackTraceExactly4000_ShouldNotTruncate()
    {
        // Arrange
        var log = CreateTestLog();
        var exactStackTrace = new string('S', 4000);

        // Act
        log.MarkAsFailed("ERR_001", "Error", exactStackTrace);

        // Assert
        log.StackTrace.Length.ShouldBe(4000);
        log.StackTrace.ShouldNotContain("TRUNCATED");
    }

    [Fact]
    public void MarkAsFailed_AfterMarkAsSuccess_ShouldOverrideToFalse()
    {
        // Arrange
        var log = CreateTestLog();
        log.MarkAsSuccess();
        log.Success.ShouldBeTrue();

        // Act
        log.MarkAsFailed("ERR_001", "Late failure");

        // Assert
        log.Success.ShouldBeFalse();
    }

    #endregion

    #region SetAdditionalContext

    [Fact]
    public void SetAdditionalContext_WithShortData_ShouldSetAsIs()
    {
        // Arrange
        var log = CreateTestLog();
        var context = "{\"orderId\":\"abc-123\",\"customerId\":\"cust-456\"}";

        // Act
        log.SetAdditionalContext(context);

        // Assert
        log.AdditionalContext.ShouldBe(context);
    }

    [Fact]
    public void SetAdditionalContext_WithNull_ShouldSetNull()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetAdditionalContext(null);

        // Assert
        log.AdditionalContext.ShouldBeNull();
    }

    [Fact]
    public void SetAdditionalContext_ExceedingMaxLength_ShouldTruncateAt4000()
    {
        // Arrange
        var log = CreateTestLog();
        var longContext = new string('C', 5000);

        // Act
        log.SetAdditionalContext(longContext);

        // Assert
        log.AdditionalContext.Length.ShouldBe(4000 + "...[TRUNCATED]".Length);
        log.AdditionalContext.ShouldEndWith("...[TRUNCATED]");
    }

    [Fact]
    public void SetAdditionalContext_Exactly4000_ShouldNotTruncate()
    {
        // Arrange
        var log = CreateTestLog();
        var exactContext = new string('C', 4000);

        // Act
        log.SetAdditionalContext(exactContext);

        // Assert
        log.AdditionalContext.Length.ShouldBe(4000);
        log.AdditionalContext.ShouldNotContain("TRUNCATED");
    }

    #endregion

    #region SetUserInfo

    [Fact]
    public void SetUserInfo_ShouldSetUserIdAndIpAddress()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetUserInfo("user-abc-123", "192.168.1.100");

        // Assert
        log.UserId.ShouldBe("user-abc-123");
        log.IpAddress.ShouldBe("192.168.1.100");
    }

    [Fact]
    public void SetUserInfo_WithNullValues_ShouldAllowNulls()
    {
        // Arrange
        var log = CreateTestLog();

        // Act
        log.SetUserInfo(null, null);

        // Assert
        log.UserId.ShouldBeNull();
        log.IpAddress.ShouldBeNull();
    }

    [Fact]
    public void SetUserInfo_CalledTwice_ShouldOverwritePreviousValues()
    {
        // Arrange
        var log = CreateTestLog();
        log.SetUserInfo("user-1", "10.0.0.1");

        // Act
        log.SetUserInfo("user-2", "10.0.0.2");

        // Assert
        log.UserId.ShouldBe("user-2");
        log.IpAddress.ShouldBe("10.0.0.2");
    }

    #endregion

    #region Full Workflow

    [Fact]
    public void FullWorkflow_SuccessfulOperation_ShouldSetAllFields()
    {
        // Arrange
        var log = PaymentOperationLog.Create(
            PaymentOperationType.InitiatePayment, "momo", "corr-xyz", TestTenantId);
        var transactionId = Guid.NewGuid();

        // Act
        log.SetTransactionInfo(transactionId, "TXN-20260219-0001");
        log.SetUserInfo("user-admin", "203.0.113.50");
        log.SetRequestData("{\"amount\":500000}");
        log.SetResponseData("{\"status\":\"success\",\"txnId\":\"MOMO-123\"}", 200);
        log.SetDuration(350);
        log.MarkAsSuccess();
        log.SetAdditionalContext("{\"retryAttempt\":0}");

        // Assert
        log.OperationType.ShouldBe(PaymentOperationType.InitiatePayment);
        log.Provider.ShouldBe("momo");
        log.CorrelationId.ShouldBe("corr-xyz");
        log.PaymentTransactionId.ShouldBe(transactionId);
        log.TransactionNumber.ShouldBe("TXN-20260219-0001");
        log.UserId.ShouldBe("user-admin");
        log.IpAddress.ShouldBe("203.0.113.50");
        log.RequestData.ShouldBe("{\"amount\":500000}");
        log.ResponseData.ShouldBe("{\"status\":\"success\",\"txnId\":\"MOMO-123\"}");
        log.HttpStatusCode.ShouldBe(200);
        log.DurationMs.ShouldBe(350);
        log.Success.ShouldBeTrue();
        log.AdditionalContext.ShouldBe("{\"retryAttempt\":0}");
        log.ErrorCode.ShouldBeNull();
        log.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void FullWorkflow_FailedOperation_ShouldSetAllErrorFields()
    {
        // Arrange
        var log = PaymentOperationLog.Create(
            PaymentOperationType.InitiateRefund, "zalopay", "corr-fail", TestTenantId);
        var refundId = Guid.NewGuid();

        // Act
        log.SetRefundInfo(refundId);
        log.SetUserInfo("user-support", "10.0.0.5");
        log.SetRequestData("{\"refundAmount\":100000}");
        log.SetResponseData("{\"error\":\"insufficient_balance\"}", 400);
        log.SetDuration(1200);
        log.MarkAsFailed("REFUND_DECLINED", "Insufficient balance for refund", "at RefundService.Process()");

        // Assert
        log.RefundId.ShouldBe(refundId);
        log.Success.ShouldBeFalse();
        log.ErrorCode.ShouldBe("REFUND_DECLINED");
        log.ErrorMessage.ShouldBe("Insufficient balance for refund");
        log.StackTrace.ShouldBe("at RefundService.Process()");
        log.HttpStatusCode.ShouldBe(400);
        log.DurationMs.ShouldBe(1200);
    }

    #endregion
}
