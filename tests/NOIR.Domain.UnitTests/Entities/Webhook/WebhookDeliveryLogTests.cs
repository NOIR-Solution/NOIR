using NOIR.Domain.Entities.Webhook;

namespace NOIR.Domain.UnitTests.Entities.Webhook;

/// <summary>
/// Unit tests verifying WebhookDeliveryLog entity behavior:
/// creation, success recording, failure with retry, and exhausted state.
/// </summary>
public class WebhookDeliveryLogTests
{
    private const string TestTenantId = "test-tenant";

    private static WebhookDeliveryLog CreateTestDeliveryLog(
        Guid? subscriptionId = null,
        string eventType = "order.created",
        Guid? eventId = null,
        string requestUrl = "https://example.com/webhook",
        string requestBody = "{\"id\":\"123\"}",
        string? requestHeaders = null,
        string? tenantId = TestTenantId)
    {
        return WebhookDeliveryLog.Create(
            subscriptionId ?? Guid.NewGuid(),
            eventType,
            eventId ?? Guid.NewGuid(),
            requestUrl,
            requestBody,
            requestHeaders,
            tenantId);
    }

    #region Create

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        // Act
        var log = WebhookDeliveryLog.Create(
            subscriptionId,
            "order.created",
            eventId,
            "https://example.com/webhook",
            "{\"orderId\":\"abc\"}",
            "{\"X-API-Key\":\"test\"}",
            TestTenantId);

        // Assert
        log.WebhookSubscriptionId.ShouldBe(subscriptionId);
        log.EventType.ShouldBe("order.created");
        log.EventId.ShouldBe(eventId);
        log.RequestUrl.ShouldBe("https://example.com/webhook");
        log.RequestBody.ShouldBe("{\"orderId\":\"abc\"}");
        log.RequestHeaders.ShouldBe("{\"X-API-Key\":\"test\"}");
        log.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldSetStatusToPending()
    {
        // Act
        var log = CreateTestDeliveryLog();

        // Assert
        log.Status.ShouldBe(WebhookDeliveryStatus.Pending);
    }

    [Fact]
    public void Create_ShouldSetAttemptNumberToOne()
    {
        // Act
        var log = CreateTestDeliveryLog();

        // Assert
        log.AttemptNumber.ShouldBe(1);
    }

    [Fact]
    public void Create_ShouldHaveNullResponseFields()
    {
        // Act
        var log = CreateTestDeliveryLog();

        // Assert
        log.ResponseStatusCode.ShouldBeNull();
        log.ResponseBody.ShouldBeNull();
        log.ResponseHeaders.ShouldBeNull();
        log.ErrorMessage.ShouldBeNull();
        log.DurationMs.ShouldBeNull();
        log.NextRetryAt.ShouldBeNull();
    }

    #endregion

    #region RecordSuccess

    [Fact]
    public void RecordSuccess_ShouldSetResponseStatusCode()
    {
        // Arrange
        var log = CreateTestDeliveryLog();

        // Act
        log.RecordSuccess(200, "{\"ok\":true}", "{\"Content-Type\":\"application/json\"}", 150);

        // Assert
        log.ResponseStatusCode.ShouldBe(200);
    }

    [Fact]
    public void RecordSuccess_ShouldSetResponseBody()
    {
        // Arrange
        var log = CreateTestDeliveryLog();

        // Act
        log.RecordSuccess(200, "{\"ok\":true}", null, 100);

        // Assert
        log.ResponseBody.ShouldBe("{\"ok\":true}");
    }

    [Fact]
    public void RecordSuccess_ShouldSetDurationMs()
    {
        // Arrange
        var log = CreateTestDeliveryLog();

        // Act
        log.RecordSuccess(200, null, null, 250);

        // Assert
        log.DurationMs.ShouldBe(250);
    }

    [Fact]
    public void RecordSuccess_ShouldSetStatusToSucceeded()
    {
        // Arrange
        var log = CreateTestDeliveryLog();

        // Act
        log.RecordSuccess(200, null, null, 100);

        // Assert
        log.Status.ShouldBe(WebhookDeliveryStatus.Succeeded);
    }

    [Fact]
    public void RecordSuccess_ShouldSetNextRetryAtToNull()
    {
        // Arrange
        var log = CreateTestDeliveryLog();

        // Act
        log.RecordSuccess(200, null, null, 100);

        // Assert
        log.NextRetryAt.ShouldBeNull();
    }

    [Fact]
    public void RecordSuccess_ShouldSetResponseHeaders()
    {
        // Arrange
        var log = CreateTestDeliveryLog();

        // Act
        log.RecordSuccess(201, null, "{\"X-Request-Id\":\"abc123\"}", 75);

        // Assert
        log.ResponseHeaders.ShouldBe("{\"X-Request-Id\":\"abc123\"}");
    }

    #endregion

    #region RecordFailure with nextRetryAt (Retrying)

    [Fact]
    public void RecordFailure_WithNextRetryAt_ShouldIncrementAttemptNumber()
    {
        // Arrange
        var log = CreateTestDeliveryLog();
        var nextRetryAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        log.RecordFailure(500, null, null, "Internal Server Error", 1000, nextRetryAt);

        // Assert
        log.AttemptNumber.ShouldBe(2);
    }

    [Fact]
    public void RecordFailure_WithNextRetryAt_ShouldSetStatusToRetrying()
    {
        // Arrange
        var log = CreateTestDeliveryLog();
        var nextRetryAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        log.RecordFailure(500, null, null, "Internal Server Error", 1000, nextRetryAt);

        // Assert
        log.Status.ShouldBe(WebhookDeliveryStatus.Retrying);
    }

    [Fact]
    public void RecordFailure_WithNextRetryAt_ShouldSetNextRetryAt()
    {
        // Arrange
        var log = CreateTestDeliveryLog();
        var nextRetryAt = DateTimeOffset.UtcNow.AddMinutes(10);

        // Act
        log.RecordFailure(503, null, null, "Service Unavailable", 5000, nextRetryAt);

        // Assert
        log.NextRetryAt.ShouldBe(nextRetryAt);
    }

    [Fact]
    public void RecordFailure_WithNextRetryAt_ShouldSetErrorMessage()
    {
        // Arrange
        var log = CreateTestDeliveryLog();
        var nextRetryAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        log.RecordFailure(500, "{\"error\":\"oops\"}", null, "Server error", 1200, nextRetryAt);

        // Assert
        log.ErrorMessage.ShouldBe("Server error");
        log.ResponseBody.ShouldBe("{\"error\":\"oops\"}");
    }

    [Fact]
    public void RecordFailure_WithNextRetryAt_CalledMultipleTimes_ShouldKeepIncrementingAttemptNumber()
    {
        // Arrange
        var log = CreateTestDeliveryLog();
        var nextRetryAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        log.RecordFailure(500, null, null, "Error 1", 100, nextRetryAt);
        log.RecordFailure(500, null, null, "Error 2", 100, nextRetryAt);
        log.RecordFailure(500, null, null, "Error 3", 100, nextRetryAt);

        // Assert
        log.AttemptNumber.ShouldBe(4);
    }

    #endregion

    #region RecordFailure without nextRetryAt (Exhausted)

    [Fact]
    public void RecordFailure_WithoutNextRetryAt_ShouldSetStatusToExhausted()
    {
        // Arrange
        var log = CreateTestDeliveryLog();

        // Act
        log.RecordFailure(500, null, null, "Final failure", 2000, null);

        // Assert
        log.Status.ShouldBe(WebhookDeliveryStatus.Exhausted);
    }

    [Fact]
    public void RecordFailure_WithoutNextRetryAt_ShouldIncrementAttemptNumber()
    {
        // Arrange
        var log = CreateTestDeliveryLog();

        // Act
        log.RecordFailure(404, "{\"error\":\"not found\"}", null, "Not found", 300, null);

        // Assert
        log.AttemptNumber.ShouldBe(2);
    }

    [Fact]
    public void RecordFailure_WithoutNextRetryAt_ShouldSetNextRetryAtToNull()
    {
        // Arrange
        var log = CreateTestDeliveryLog();

        // Act
        log.RecordFailure(500, null, null, "Final failure", 1000, null);

        // Assert
        log.NextRetryAt.ShouldBeNull();
    }

    [Fact]
    public void RecordFailure_WithNullStatusCode_ShouldSetResponseStatusCodeToNull()
    {
        // Arrange
        var log = CreateTestDeliveryLog();
        var nextRetryAt = DateTimeOffset.UtcNow.AddMinutes(5);

        // Act
        log.RecordFailure(null, null, null, "Connection timeout", 30000, nextRetryAt);

        // Assert
        log.ResponseStatusCode.ShouldBeNull();
    }

    #endregion
}
