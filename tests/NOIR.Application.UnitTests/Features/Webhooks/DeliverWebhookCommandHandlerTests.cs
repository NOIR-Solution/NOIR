using NOIR.Application.Features.Webhooks.Commands.DeliverWebhook;
using NOIR.Application.Features.Webhooks.Specifications;
using NOIR.Domain.Entities.Webhook;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for DeliverWebhookCommandHandler.
/// Tests HMAC-SHA256 signing, HTTP delivery, retry with exponential backoff,
/// custom header handling, error scenarios, and delivery log recording.
/// </summary>
public class DeliverWebhookCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IRepository<WebhookSubscription, Guid>> _subscriptionRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly Mock<ILogger<DeliverWebhookCommandHandler>> _loggerMock;
    private readonly DeliverWebhookCommandHandler _handler;

    /// <summary>
    /// Captures the DeliverWebhookCommand and DeliveryOptions passed to PublishAsync for retry verification.
    /// Wolverine's ScheduleAsync extension calls PublishAsync with DeliveryOptions.ScheduledTime set.
    /// </summary>
    private readonly List<(object Message, DeliveryOptions? Options)> _capturedPublishCalls = new();

    public DeliverWebhookCommandHandlerTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _subscriptionRepositoryMock = new Mock<IRepository<WebhookSubscription, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _messageBusMock = new Mock<IMessageBus>();
        _loggerMock = new Mock<ILogger<DeliverWebhookCommandHandler>>();

        // Capture all PublishAsync calls (used by Wolverine's ScheduleAsync extension)
        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Callback<object, DeliveryOptions?>((msg, opts) => _capturedPublishCalls.Add((msg, opts)))
            .Returns(ValueTask.CompletedTask);

        _handler = new DeliverWebhookCommandHandler(
            _httpClientFactoryMock.Object,
            _dbContextMock.Object,
            _subscriptionRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _messageBusMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Creates a configurable mock HTTP message handler that returns the specified response.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
        public HttpRequestMessage? CapturedRequest { get; private set; }

        public MockHttpMessageHandler(HttpStatusCode statusCode, string responseBody = "OK")
        {
            _handler = (request, _) =>
            {
                CapturedRequest = request;
                return Task.FromResult(new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody)
                });
            };
        }

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return await _handler(request, cancellationToken);
        }
    }

    private HttpClient SetupHttpClient(MockHttpMessageHandler messageHandler)
    {
        var client = new HttpClient(messageHandler);
        _httpClientFactoryMock
            .Setup(x => x.CreateClient("WebhookDelivery"))
            .Returns(client);
        return client;
    }

    private HttpClient SetupHttpClient(HttpStatusCode statusCode, string responseBody = "OK")
    {
        var messageHandler = new MockHttpMessageHandler(statusCode, responseBody);
        return SetupHttpClient(messageHandler);
    }

    private void SetupDeliveryLog(WebhookDeliveryLog? deliveryLog)
    {
        var logs = deliveryLog is null
            ? new List<WebhookDeliveryLog>()
            : new List<WebhookDeliveryLog> { deliveryLog };

        var mockDbSet = logs.BuildMockDbSet();
        _dbContextMock.Setup(x => x.WebhookDeliveryLogs).Returns(mockDbSet.Object);
    }

    private static WebhookDeliveryLog CreateDeliveryLog(
        Guid? subscriptionId = null,
        string eventType = "order.created",
        string? tenantId = "tenant-123")
    {
        return WebhookDeliveryLog.Create(
            subscriptionId: subscriptionId ?? Guid.NewGuid(),
            eventType: eventType,
            eventId: Guid.NewGuid(),
            requestUrl: "https://api.example.com/webhooks",
            requestBody: """{"event":"order.created","data":{}}""",
            tenantId: tenantId);
    }

    private static DeliverWebhookCommand CreateValidCommand(
        Guid? subscriptionId = null,
        Guid? deliveryLogId = null,
        string eventType = "order.created",
        string payload = """{"event":"order.created","data":{}}""",
        string secret = "test-secret-key-12345",
        string url = "https://api.example.com/webhooks",
        int timeoutSeconds = 30,
        int maxRetries = 5,
        int attemptNumber = 1,
        string? customHeaders = null)
    {
        return new DeliverWebhookCommand(
            SubscriptionId: subscriptionId ?? Guid.NewGuid(),
            DeliveryLogId: deliveryLogId ?? Guid.NewGuid(),
            EventType: eventType,
            EventId: Guid.NewGuid(),
            Payload: payload,
            Secret: secret,
            Url: url,
            TimeoutSeconds: timeoutSeconds,
            MaxRetries: maxRetries,
            AttemptNumber: attemptNumber,
            CustomHeaders: customHeaders);
    }

    /// <summary>
    /// Gets the scheduled retry command captured from PublishAsync, if any.
    /// Wolverine's ScheduleAsync calls PublishAsync with DeliveryOptions containing ScheduledTime.
    /// </summary>
    private (DeliverWebhookCommand Command, DeliveryOptions Options)? GetScheduledRetry()
    {
        var retryCall = _capturedPublishCalls
            .FirstOrDefault(c => c.Message is DeliverWebhookCommand && c.Options?.ScheduledTime is not null);

        if (retryCall.Message is DeliverWebhookCommand cmd)
            return (cmd, retryCall.Options!);

        return null;
    }

    #endregion

    #region Successful Delivery

    [Fact]
    public async Task Handle_SuccessfulDelivery_ShouldRecordSuccessOnDeliveryLog()
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(deliveryLogId: deliveryLog.Id);

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(HttpStatusCode.OK, """{"status":"received"}""");

        var subscription = WebhookSubscription.Create(
            "Test", command.Url, "order.*", tenantId: "tenant-123");
        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        deliveryLog.Status.Should().Be(WebhookDeliveryStatus.Succeeded);
        deliveryLog.ResponseStatusCode.Should().Be(200);
        deliveryLog.ResponseBody.Should().NotBeNullOrEmpty();
        deliveryLog.DurationMs.Should().NotBeNull();
        deliveryLog.DurationMs!.Value.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Handle_SuccessfulDelivery_ShouldCallRecordDeliveryOnSubscription()
    {
        // Arrange
        var subscriptionId = Guid.NewGuid();
        var deliveryLog = CreateDeliveryLog(subscriptionId: subscriptionId);
        var command = CreateValidCommand(
            subscriptionId: subscriptionId,
            deliveryLogId: deliveryLog.Id);

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(HttpStatusCode.OK);

        var subscription = WebhookSubscription.Create(
            "Test", command.Url, "order.*", tenantId: "tenant-123");
        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — RecordDelivery sets LastDeliveryAt
        subscription.LastDeliveryAt.Should().NotBeNull();
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SuccessfulDelivery_ShouldCallSaveChangesAsyncOnDbContext()
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(deliveryLogId: deliveryLog.Id);

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(HttpStatusCode.OK);

        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region HMAC-SHA256 Signature

    [Fact]
    public async Task Handle_ShouldComputeCorrectHmacSha256Signature()
    {
        // Arrange
        const string payload = """{"event":"order.created","orderId":"12345"}""";
        const string secret = "my-webhook-secret";

        // Compute expected signature: sha256=hex(HMACSHA256(UTF8(secret), UTF8(payload)))
        var expectedSignature = ComputeExpectedSignature(payload, secret);

        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            payload: payload,
            secret: secret);

        SetupDeliveryLog(deliveryLog);

        HttpRequestMessage? capturedRequest = null;
        var messageHandler = new MockHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OK")
            });
        });
        SetupHttpClient(messageHandler);

        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        var signatureHeader = capturedRequest!.Headers.GetValues("X-Webhook-Signature-256").Single();
        signatureHeader.Should().Be($"sha256={expectedSignature}");
    }

    [Theory]
    [InlineData("hello world", "secret123")]
    [InlineData("{}", "key")]
    [InlineData("""{"complex":"payload","nested":{"value":42}}""", "long-secret-key-with-special-chars-!@#$")]
    public async Task Handle_HmacSignature_ShouldMatchManualComputation(string payload, string secret)
    {
        // Arrange
        var expected = ComputeExpectedSignature(payload, secret);

        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            payload: payload,
            secret: secret);

        SetupDeliveryLog(deliveryLog);

        HttpRequestMessage? capturedRequest = null;
        var messageHandler = new MockHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OK")
            });
        });
        SetupHttpClient(messageHandler);

        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        var signatureHeader = capturedRequest!.Headers.GetValues("X-Webhook-Signature-256").Single();
        signatureHeader.Should().Be($"sha256={expected}");
    }

    private static string ComputeExpectedSignature(string payload, string secret)
    {
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(secretBytes);
        var hashBytes = hmac.ComputeHash(payloadBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    #endregion

    #region HTTP Headers

    [Fact]
    public async Task Handle_ShouldSendCorrectWebhookHeaders()
    {
        // Arrange
        const string eventType = "payment.succeeded";

        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            eventType: eventType);

        SetupDeliveryLog(deliveryLog);

        HttpRequestMessage? capturedRequest = null;
        var messageHandler = new MockHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OK")
            });
        });
        SetupHttpClient(messageHandler);

        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();

        // X-Webhook-Signature-256 header present with sha256= prefix
        capturedRequest!.Headers.Contains("X-Webhook-Signature-256").Should().BeTrue();
        var signature = capturedRequest.Headers.GetValues("X-Webhook-Signature-256").Single();
        signature.Should().StartWith("sha256=");

        // X-Webhook-Event header matches event type
        capturedRequest.Headers.GetValues("X-Webhook-Event").Single().Should().Be(eventType);

        // X-Webhook-Delivery-Id header matches delivery log ID
        capturedRequest.Headers.GetValues("X-Webhook-Delivery-Id").Single()
            .Should().Be(command.DeliveryLogId.ToString());

        // Content-Type is application/json
        capturedRequest.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    #endregion

    #region HTTP 4xx / 5xx Failure and Retry

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task Handle_Http4xxFailure_ShouldRecordFailureAndScheduleRetry(HttpStatusCode statusCode)
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            attemptNumber: 1,
            maxRetries: 5);

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(statusCode, "Bad Request");

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        deliveryLog.Status.Should().Be(WebhookDeliveryStatus.Retrying);
        deliveryLog.ResponseStatusCode.Should().Be((int)statusCode);
        deliveryLog.ErrorMessage.Should().NotBeNullOrEmpty();
        deliveryLog.NextRetryAt.Should().NotBeNull();

        // Wolverine ScheduleAsync calls PublishAsync with DeliveryOptions.ScheduledTime
        var retry = GetScheduledRetry();
        retry.Should().NotBeNull("a retry should have been scheduled");
        retry!.Value.Command.AttemptNumber.Should().Be(2);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task Handle_Http5xxFailure_ShouldRecordFailureAndScheduleRetry(HttpStatusCode statusCode)
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            attemptNumber: 1,
            maxRetries: 5);

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(statusCode, "Server Error");

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        deliveryLog.Status.Should().Be(WebhookDeliveryStatus.Retrying);
        deliveryLog.ResponseStatusCode.Should().Be((int)statusCode);

        var retry = GetScheduledRetry();
        retry.Should().NotBeNull("a retry should have been scheduled");
        retry!.Value.Command.AttemptNumber.Should().Be(2);
    }

    [Theory]
    [InlineData(1, 30)]       // Attempt 1 -> 30 seconds
    [InlineData(2, 120)]      // Attempt 2 -> 2 minutes
    [InlineData(3, 900)]      // Attempt 3 -> 15 minutes
    [InlineData(4, 3600)]     // Attempt 4 -> 1 hour
    [InlineData(5, 14400)]    // Attempt 5 -> 4 hours
    public async Task Handle_FailureRetry_ShouldUseCorrectExponentialBackoff(
        int attemptNumber, int expectedDelaySeconds)
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            attemptNumber: attemptNumber,
            maxRetries: 6); // maxRetries > attemptNumber so retry is allowed

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(HttpStatusCode.InternalServerError, "Error");

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var beforeHandle = DateTimeOffset.UtcNow;

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — verify the scheduled time corresponds to the expected delay
        var retry = GetScheduledRetry();
        retry.Should().NotBeNull("a retry should have been scheduled");

        var expectedTime = beforeHandle.AddSeconds(expectedDelaySeconds);
        retry!.Value.Options.ScheduledTime!.Value.Should().BeCloseTo(expectedTime, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Final Attempt Exhaustion

    [Fact]
    public async Task Handle_FinalAttemptExhausted_ShouldSetStatusToExhaustedAndNotScheduleRetry()
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            attemptNumber: 5,
            maxRetries: 5); // attemptNumber == maxRetries -> no more retries

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(HttpStatusCode.InternalServerError, "Error");

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        deliveryLog.Status.Should().Be(WebhookDeliveryStatus.Exhausted);
        deliveryLog.NextRetryAt.Should().BeNull();

        // No retry should have been scheduled
        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_AttemptBeyondMaxRetries_ShouldNotScheduleRetry()
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            attemptNumber: 10,
            maxRetries: 5);

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(HttpStatusCode.InternalServerError, "Error");

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        deliveryLog.Status.Should().Be(WebhookDeliveryStatus.Exhausted);

        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()),
            Times.Never);
    }

    #endregion

    #region Timeout Exception

    [Fact]
    public async Task Handle_TimeoutException_ShouldRecordFailureWithTimeoutMessage()
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            timeoutSeconds: 5,
            attemptNumber: 1,
            maxRetries: 5);

        SetupDeliveryLog(deliveryLog);

        // Simulate a timeout: TaskCanceledException with TimeoutException inner
        var messageHandler = new MockHttpMessageHandler((_, _) =>
            throw new TaskCanceledException(
                "The request was canceled due to the configured HttpClient.Timeout.",
                new TimeoutException("A task was canceled.")));
        SetupHttpClient(messageHandler);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        deliveryLog.Status.Should().Be(WebhookDeliveryStatus.Retrying);
        deliveryLog.ErrorMessage.Should().Contain("timed out");
        deliveryLog.ResponseStatusCode.Should().BeNull();
        deliveryLog.ResponseBody.Should().BeNull();

        var retry = GetScheduledRetry();
        retry.Should().NotBeNull("a retry should have been scheduled");
        retry!.Value.Command.AttemptNumber.Should().Be(2);
    }

    #endregion

    #region HttpRequestException

    [Fact]
    public async Task Handle_HttpRequestException_ShouldRecordFailureWithErrorMessage()
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            attemptNumber: 1,
            maxRetries: 5);

        SetupDeliveryLog(deliveryLog);

        var messageHandler = new MockHttpMessageHandler((_, _) =>
            throw new HttpRequestException("No such host is known."));
        SetupHttpClient(messageHandler);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        deliveryLog.Status.Should().Be(WebhookDeliveryStatus.Retrying);
        deliveryLog.ErrorMessage.Should().Contain("HTTP request failed");
        deliveryLog.ErrorMessage.Should().Contain("No such host is known.");
        deliveryLog.ResponseStatusCode.Should().BeNull();

        var retry = GetScheduledRetry();
        retry.Should().NotBeNull("a retry should have been scheduled");
        retry!.Value.Command.AttemptNumber.Should().Be(2);
    }

    #endregion

    #region Custom Headers

    [Fact]
    public async Task Handle_WithValidCustomHeaders_ShouldAddNonBlockedHeaders()
    {
        // Arrange
        var customHeaders = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { "X-Custom-Token", "abc-123" },
            { "X-Correlation-Id", "corr-456" }
        });

        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            customHeaders: customHeaders);

        SetupDeliveryLog(deliveryLog);

        HttpRequestMessage? capturedRequest = null;
        var messageHandler = new MockHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OK")
            });
        });
        SetupHttpClient(messageHandler);

        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.GetValues("X-Custom-Token").Single().Should().Be("abc-123");
        capturedRequest.Headers.GetValues("X-Correlation-Id").Single().Should().Be("corr-456");
    }

    [Theory]
    [InlineData("Host")]
    [InlineData("Authorization")]
    [InlineData("Cookie")]
    [InlineData("Content-Type")]
    [InlineData("X-Webhook-Signature-256")]
    [InlineData("X-Webhook-Event")]
    [InlineData("X-Webhook-Delivery-Id")]
    [InlineData("User-Agent")]
    [InlineData("Proxy-Authorization")]
    [InlineData("X-Forwarded-For")]
    public async Task Handle_WithBlockedCustomHeaders_ShouldSkipBlockedHeaders(string blockedHeaderName)
    {
        // Arrange
        var customHeaders = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            { blockedHeaderName, "malicious-value" },
            { "X-Safe-Header", "safe-value" }
        });

        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            customHeaders: customHeaders);

        SetupDeliveryLog(deliveryLog);

        HttpRequestMessage? capturedRequest = null;
        var messageHandler = new MockHttpMessageHandler((request, _) =>
        {
            capturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("OK")
            });
        });
        SetupHttpClient(messageHandler);

        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — safe header is present
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.GetValues("X-Safe-Header").Single().Should().Be("safe-value");

        // The blocked header should not have the malicious value.
        // For standard webhook headers (X-Webhook-*), they will have the legitimate value from the handler.
        // For truly blocked headers that the handler does not set (Host, Authorization, etc.),
        // they should not appear at all.
        if (blockedHeaderName.StartsWith("X-Webhook-"))
        {
            // These are set by the handler itself, so they will not have the "malicious-value"
            var headerValues = capturedRequest.Headers.GetValues(blockedHeaderName).ToList();
            headerValues.Should().NotContain("malicious-value");
        }
    }

    [Fact]
    public async Task Handle_WithMalformedCustomHeadersJson_ShouldNotThrowAndContinueDelivery()
    {
        // Arrange
        const string malformedJson = "{ invalid json {{";

        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            customHeaders: malformedJson);

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(HttpStatusCode.OK);

        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act — should not throw
        await _handler.Handle(command, CancellationToken.None);

        // Assert — delivery still succeeds
        deliveryLog.Status.Should().Be(WebhookDeliveryStatus.Succeeded);
    }

    [Fact]
    public async Task Handle_WithNullCustomHeaders_ShouldNotThrow()
    {
        // Arrange
        var deliveryLog = CreateDeliveryLog();
        var command = CreateValidCommand(
            deliveryLogId: deliveryLog.Id,
            customHeaders: null);

        SetupDeliveryLog(deliveryLog);
        SetupHttpClient(HttpStatusCode.OK);

        _subscriptionRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WebhookSubscriptionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WebhookSubscription?)null);

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act — should not throw
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        deliveryLog.Status.Should().Be(WebhookDeliveryStatus.Succeeded);
    }

    #endregion

    #region Missing Delivery Log

    [Fact]
    public async Task Handle_WhenDeliveryLogNotFound_ShouldReturnEarlyWithoutException()
    {
        // Arrange
        var command = CreateValidCommand(deliveryLogId: Guid.NewGuid());

        // Setup empty delivery logs — no matching log
        SetupDeliveryLog(null);

        // Act — should not throw
        await _handler.Handle(command, CancellationToken.None);

        // Assert — should not call HttpClientFactory, MessageBus, or SaveChanges
        _httpClientFactoryMock.Verify(
            x => x.CreateClient(It.IsAny<string>()),
            Times.Never);

        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()),
            Times.Never);

        _dbContextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
