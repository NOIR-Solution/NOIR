using NOIR.Domain.Entities.Shipping;

namespace NOIR.Domain.UnitTests.Entities.Shipping;

/// <summary>
/// Unit tests for the ShippingWebhookLog entity.
/// Tests factory method, processing lifecycle (success/failure),
/// retry tracking, and property setters.
/// </summary>
public class ShippingWebhookLogTests
{
    private const ShippingProviderCode TestProviderCode = ShippingProviderCode.GHTK;
    private const string TestEndpoint = "/api/webhooks/shipping/ghtk";
    private const string TestBody = """{"tracking_number":"TRK-001","status":"delivered"}""";
    private const string TestTrackingNumber = "TRK-001";
    private const string TestHeadersJson = """{"X-Signature":"abc123","Content-Type":"application/json"}""";
    private const string TestSignature = "sha256=abc123def456";

    /// <summary>
    /// Helper to create a default valid webhook log for tests.
    /// </summary>
    private static ShippingWebhookLog CreateTestWebhookLog(
        ShippingProviderCode providerCode = TestProviderCode,
        string endpoint = TestEndpoint,
        string body = TestBody,
        string? trackingNumber = null,
        string? headersJson = null,
        string? signature = null,
        string httpMethod = "POST")
    {
        return ShippingWebhookLog.Create(
            providerCode,
            endpoint,
            body,
            trackingNumber,
            headersJson,
            signature,
            httpMethod);
    }

    #region Create Factory Method

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidLog()
    {
        // Act
        var log = ShippingWebhookLog.Create(
            TestProviderCode,
            TestEndpoint,
            TestBody);

        // Assert
        log.Should().NotBeNull();
        log.Id.Should().NotBe(Guid.Empty);
        log.ProviderCode.Should().Be(TestProviderCode);
        log.Endpoint.Should().Be(TestEndpoint);
        log.Body.Should().Be(TestBody);
    }

    [Fact]
    public void Create_ShouldDefaultProcessedSuccessfullyToFalse()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.ProcessedSuccessfully.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldDefaultProcessingAttemptsToZero()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.ProcessingAttempts.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldSetReceivedAtToCurrentUtcTime()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.ReceivedAt.Should().BeOnOrAfter(beforeCreate);
        log.ReceivedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_ShouldDefaultProcessedAtToNull()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldDefaultErrorMessageToNull()
    {
        // Act
        var log = CreateTestWebhookLog();

        // Assert
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldDefaultHttpMethodToPost()
    {
        // Act
        var log = ShippingWebhookLog.Create(TestProviderCode, TestEndpoint, TestBody);

        // Assert
        log.HttpMethod.Should().Be("POST");
    }

    [Fact]
    public void Create_WithCustomHttpMethod_ShouldSetMethod()
    {
        // Act
        var log = CreateTestWebhookLog(httpMethod: "PUT");

        // Assert
        log.HttpMethod.Should().Be("PUT");
    }

    [Fact]
    public void Create_WithTrackingNumber_ShouldSetTrackingNumber()
    {
        // Act
        var log = CreateTestWebhookLog(trackingNumber: TestTrackingNumber);

        // Assert
        log.TrackingNumber.Should().Be(TestTrackingNumber);
    }

    [Fact]
    public void Create_WithoutTrackingNumber_ShouldHaveNullTrackingNumber()
    {
        // Act
        var log = CreateTestWebhookLog(trackingNumber: null);

        // Assert
        log.TrackingNumber.Should().BeNull();
    }

    [Fact]
    public void Create_WithHeadersJson_ShouldSetHeaders()
    {
        // Act
        var log = CreateTestWebhookLog(headersJson: TestHeadersJson);

        // Assert
        log.HeadersJson.Should().Be(TestHeadersJson);
    }

    [Fact]
    public void Create_WithoutHeadersJson_ShouldHaveNullHeaders()
    {
        // Act
        var log = CreateTestWebhookLog(headersJson: null);

        // Assert
        log.HeadersJson.Should().BeNull();
    }

    [Fact]
    public void Create_WithSignature_ShouldSetSignature()
    {
        // Act
        var log = CreateTestWebhookLog(signature: TestSignature);

        // Assert
        log.Signature.Should().Be(TestSignature);
    }

    [Fact]
    public void Create_WithoutSignature_ShouldHaveNullSignature()
    {
        // Act
        var log = CreateTestWebhookLog(signature: null);

        // Assert
        log.Signature.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllOptionalParameters_ShouldSetAllFields()
    {
        // Act
        var log = ShippingWebhookLog.Create(
            TestProviderCode,
            TestEndpoint,
            TestBody,
            TestTrackingNumber,
            TestHeadersJson,
            TestSignature,
            "POST");

        // Assert
        log.ProviderCode.Should().Be(TestProviderCode);
        log.Endpoint.Should().Be(TestEndpoint);
        log.Body.Should().Be(TestBody);
        log.TrackingNumber.Should().Be(TestTrackingNumber);
        log.HeadersJson.Should().Be(TestHeadersJson);
        log.Signature.Should().Be(TestSignature);
        log.HttpMethod.Should().Be("POST");
    }

    [Theory]
    [InlineData(ShippingProviderCode.GHTK)]
    [InlineData(ShippingProviderCode.GHN)]
    [InlineData(ShippingProviderCode.JTExpress)]
    [InlineData(ShippingProviderCode.ViettelPost)]
    [InlineData(ShippingProviderCode.NinjaVan)]
    [InlineData(ShippingProviderCode.VNPost)]
    [InlineData(ShippingProviderCode.BestExpress)]
    [InlineData(ShippingProviderCode.Custom)]
    public void Create_WithDifferentProviderCodes_ShouldSetCorrectCode(ShippingProviderCode code)
    {
        // Act
        var log = CreateTestWebhookLog(providerCode: code);

        // Assert
        log.ProviderCode.Should().Be(code);
    }

    [Fact]
    public void Create_MultipleLogs_ShouldGenerateUniqueIds()
    {
        // Act
        var log1 = CreateTestWebhookLog();
        var log2 = CreateTestWebhookLog();

        // Assert
        log1.Id.Should().NotBe(log2.Id);
    }

    #endregion

    #region MarkAsProcessed

    [Fact]
    public void MarkAsProcessed_ShouldSetProcessedSuccessfullyToTrue()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsProcessed();

        // Assert
        log.ProcessedSuccessfully.Should().BeTrue();
    }

    [Fact]
    public void MarkAsProcessed_ShouldSetProcessedAtTimestamp()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        var beforeProcess = DateTimeOffset.UtcNow;

        // Act
        log.MarkAsProcessed();

        // Assert
        log.ProcessedAt.Should().NotBeNull();
        log.ProcessedAt.Should().BeOnOrAfter(beforeProcess);
    }

    [Fact]
    public void MarkAsProcessed_ShouldIncrementProcessingAttempts()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.ProcessingAttempts.Should().Be(0);

        // Act
        log.MarkAsProcessed();

        // Assert
        log.ProcessingAttempts.Should().Be(1);
    }

    [Fact]
    public void MarkAsProcessed_AfterPreviousFailure_ShouldSetSuccessful()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.MarkAsFailed("First attempt failed");
        log.ProcessedSuccessfully.Should().BeFalse();

        // Act
        log.MarkAsProcessed();

        // Assert
        log.ProcessedSuccessfully.Should().BeTrue();
        log.ProcessingAttempts.Should().Be(2);
    }

    #endregion

    #region MarkAsFailed

    [Fact]
    public void MarkAsFailed_ShouldSetProcessedSuccessfullyToFalse()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsFailed("Connection timeout");

        // Assert
        log.ProcessedSuccessfully.Should().BeFalse();
    }

    [Fact]
    public void MarkAsFailed_ShouldSetErrorMessage()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsFailed("Invalid signature: expected abc, got xyz");

        // Assert
        log.ErrorMessage.Should().Be("Invalid signature: expected abc, got xyz");
    }

    [Fact]
    public void MarkAsFailed_ShouldSetProcessedAtTimestamp()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        var beforeFail = DateTimeOffset.UtcNow;

        // Act
        log.MarkAsFailed("Error occurred");

        // Assert
        log.ProcessedAt.Should().NotBeNull();
        log.ProcessedAt.Should().BeOnOrAfter(beforeFail);
    }

    [Fact]
    public void MarkAsFailed_ShouldIncrementProcessingAttempts()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsFailed("Error");

        // Assert
        log.ProcessingAttempts.Should().Be(1);
    }

    [Fact]
    public void MarkAsFailed_MultipleFailures_ShouldTrackAttemptCount()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsFailed("First failure");
        log.MarkAsFailed("Second failure");
        log.MarkAsFailed("Third failure");

        // Assert
        log.ProcessingAttempts.Should().Be(3);
        log.ErrorMessage.Should().Be("Third failure");
    }

    [Fact]
    public void MarkAsFailed_ShouldOverwritePreviousErrorMessage()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        log.MarkAsFailed("First error");

        // Act
        log.MarkAsFailed("Different error on retry");

        // Assert
        log.ErrorMessage.Should().Be("Different error on retry");
    }

    #endregion

    #region SetTrackingNumber

    [Fact]
    public void SetTrackingNumber_ShouldSetTrackingNumber()
    {
        // Arrange
        var log = CreateTestWebhookLog(trackingNumber: null);
        log.TrackingNumber.Should().BeNull();

        // Act
        log.SetTrackingNumber("TRK-EXTRACTED-001");

        // Assert
        log.TrackingNumber.Should().Be("TRK-EXTRACTED-001");
    }

    [Fact]
    public void SetTrackingNumber_ShouldOverwritePreviousValue()
    {
        // Arrange
        var log = CreateTestWebhookLog(trackingNumber: "TRK-OLD");

        // Act
        log.SetTrackingNumber("TRK-NEW");

        // Assert
        log.TrackingNumber.Should().Be("TRK-NEW");
    }

    #endregion

    #region Processing Lifecycle

    [Fact]
    public void ProcessingLifecycle_FailThenSucceed_ShouldTrackCorrectState()
    {
        // Arrange
        var log = CreateTestWebhookLog(
            trackingNumber: TestTrackingNumber,
            headersJson: TestHeadersJson,
            signature: TestSignature);

        // Assert initial state
        log.ProcessedSuccessfully.Should().BeFalse();
        log.ProcessingAttempts.Should().Be(0);
        log.ProcessedAt.Should().BeNull();
        log.ErrorMessage.Should().BeNull();

        // Act - first attempt fails
        log.MarkAsFailed("Signature verification failed");
        log.ProcessedSuccessfully.Should().BeFalse();
        log.ProcessingAttempts.Should().Be(1);
        log.ProcessedAt.Should().NotBeNull();
        log.ErrorMessage.Should().Be("Signature verification failed");

        // Act - second attempt fails
        log.MarkAsFailed("Timeout connecting to database");
        log.ProcessedSuccessfully.Should().BeFalse();
        log.ProcessingAttempts.Should().Be(2);
        log.ErrorMessage.Should().Be("Timeout connecting to database");

        // Act - third attempt succeeds
        log.MarkAsProcessed();
        log.ProcessedSuccessfully.Should().BeTrue();
        log.ProcessingAttempts.Should().Be(3);
    }

    [Fact]
    public void ProcessingLifecycle_ImmediateSuccess_ShouldSetCorrectState()
    {
        // Arrange
        var log = CreateTestWebhookLog();

        // Act
        log.MarkAsProcessed();

        // Assert
        log.ProcessedSuccessfully.Should().BeTrue();
        log.ProcessingAttempts.Should().Be(1);
        log.ProcessedAt.Should().NotBeNull();
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ProcessingLifecycle_ExtractTrackingThenProcess()
    {
        // Arrange
        var log = CreateTestWebhookLog(trackingNumber: null);

        // Act - extract tracking number from payload
        log.SetTrackingNumber("TRK-PARSED-999");

        // Act - process successfully
        log.MarkAsProcessed();

        // Assert
        log.TrackingNumber.Should().Be("TRK-PARSED-999");
        log.ProcessedSuccessfully.Should().BeTrue();
        log.ProcessingAttempts.Should().Be(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithEmptyBody_ShouldAllowEmptyBody()
    {
        // Act
        var log = CreateTestWebhookLog(body: "");

        // Assert
        log.Body.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithLargeBody_ShouldStoreCompletely()
    {
        // Arrange - simulate a large webhook payload
        var largeBody = new string('x', 50_000);

        // Act
        var log = CreateTestWebhookLog(body: largeBody);

        // Assert
        log.Body.Should().HaveLength(50_000);
    }

    [Fact]
    public void MarkAsFailed_WithLongErrorMessage_ShouldStoreCompletely()
    {
        // Arrange
        var log = CreateTestWebhookLog();
        var longError = "Error: " + new string('x', 5_000);

        // Act
        log.MarkAsFailed(longError);

        // Assert
        log.ErrorMessage.Should().StartWith("Error: ");
        log.ErrorMessage.Should().HaveLength(5_007);
    }

    #endregion
}
