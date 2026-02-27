using NOIR.Application.Features.Webhooks;
using NOIR.Application.Features.Webhooks.Commands.DeliverWebhook;
using NOIR.Application.Features.Webhooks.Services;
using NOIR.Application.Features.Webhooks.Specifications;
using NOIR.Domain.Entities.Webhook;
using NOIR.Domain.Events.Product;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for WebhookDispatcher — the fan-out service that bridges domain events
/// to webhook delivery by finding matching subscriptions, creating delivery logs,
/// and publishing DeliverWebhookCommand messages via the message bus.
/// </summary>
public class WebhookDispatcherTests
{
    #region Test Setup

    private readonly Mock<IRepository<WebhookSubscription, Guid>> _repositoryMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly WebhookEventTypeRegistry _registry;
    private readonly Mock<IMessageBus> _messageBusMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<WebhookDispatcher>> _loggerMock;
    private readonly Mock<DbSet<WebhookDeliveryLog>> _deliveryLogDbSetMock;
    private readonly List<WebhookDeliveryLog> _capturedDeliveryLogs;
    private readonly WebhookDispatcher _dispatcher;

    public WebhookDispatcherTests()
    {
        _repositoryMock = new Mock<IRepository<WebhookSubscription, Guid>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _registry = new WebhookEventTypeRegistry();
        _messageBusMock = new Mock<IMessageBus>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<WebhookDispatcher>>();

        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-123");

        // Set up the DbSet mock to capture Add calls
        _capturedDeliveryLogs = new List<WebhookDeliveryLog>();
        _deliveryLogDbSetMock = new Mock<DbSet<WebhookDeliveryLog>>();
        _deliveryLogDbSetMock
            .Setup(x => x.Add(It.IsAny<WebhookDeliveryLog>()))
            .Callback<WebhookDeliveryLog>(log => _capturedDeliveryLogs.Add(log));
        _dbContextMock
            .Setup(x => x.WebhookDeliveryLogs)
            .Returns(_deliveryLogDbSetMock.Object);

        _dispatcher = new WebhookDispatcher(
            _repositoryMock.Object,
            _registry,
            _dbContextMock.Object,
            _messageBusMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Helper: creates an active subscription with the given event pattern and URL.
    /// </summary>
    private static WebhookSubscription CreateSubscription(
        string eventPatterns = "product.*",
        string url = "https://api.example.com/webhooks",
        string name = "Test Webhook",
        string? tenantId = "tenant-123")
    {
        return WebhookSubscription.Create(name, url, eventPatterns, tenantId: tenantId);
    }

    /// <summary>
    /// Helper: sets up the repository to return the given subscriptions for ActiveWebhookSubscriptionsSpec.
    /// </summary>
    private void SetupActiveSubscriptions(params WebhookSubscription[] subscriptions)
    {
        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ActiveWebhookSubscriptionsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions.ToList());
    }

    #endregion

    #region Unknown Domain Event (Not in Registry)

    [Fact]
    public async Task DispatchAsync_UnknownDomainEvent_ShouldSkipDispatchAndNotQuerySubscriptions()
    {
        // Arrange — a domain event type not registered in WebhookEventTypeRegistry
        var unknownEvent = new UnregisteredTestEvent();

        // Act
        await _dispatcher.DispatchAsync(unknownEvent, CancellationToken.None);

        // Assert — should not query subscriptions at all
        _repositoryMock.Verify(
            x => x.ListAsync(
                It.IsAny<ActiveWebhookSubscriptionsSpec>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _dbContextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()),
            Times.Never);
    }

    #endregion

    #region No Active Subscriptions

    [Fact]
    public async Task DispatchAsync_NoActiveSubscriptions_ShouldNotCreateDeliveryLogsOrPublishCommands()
    {
        // Arrange
        var domainEvent = new ProductCreatedEvent(Guid.NewGuid(), "Test Product", "test-product");
        SetupActiveSubscriptions(); // empty list

        // Act
        await _dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert
        _capturedDeliveryLogs.Should().BeEmpty();

        _dbContextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()),
            Times.Never);
    }

    #endregion

    #region Active Subscriptions But None Match Event Pattern

    [Fact]
    public async Task DispatchAsync_ActiveSubscriptionsButNoneMatchEventPattern_ShouldNotCreateDeliveryLogs()
    {
        // Arrange — subscription listens for "order.*" but event is "product.created"
        var subscription = CreateSubscription(eventPatterns: "order.*");
        SetupActiveSubscriptions(subscription);

        var domainEvent = new ProductCreatedEvent(Guid.NewGuid(), "Test Product", "test-product");

        // Act
        await _dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert
        _capturedDeliveryLogs.Should().BeEmpty();

        _dbContextMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        _messageBusMock.Verify(
            x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()),
            Times.Never);
    }

    #endregion

    #region Single Matching Subscription — Delivery Log Fields

    [Fact]
    public async Task DispatchAsync_SingleMatchingSubscription_ShouldCreateDeliveryLogWithCorrectFields()
    {
        // Arrange
        var subscription = CreateSubscription(
            eventPatterns: "product.*",
            url: "https://hooks.example.com/notify");
        SetupActiveSubscriptions(subscription);

        var productId = Guid.NewGuid();
        var domainEvent = new ProductCreatedEvent(productId, "Awesome Widget", "awesome-widget");

        // Act
        await _dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert — exactly one delivery log created
        _capturedDeliveryLogs.Should().HaveCount(1);
        var log = _capturedDeliveryLogs[0];

        log.WebhookSubscriptionId.Should().Be(subscription.Id);
        log.EventType.Should().Be("product.created");
        log.EventId.Should().Be(domainEvent.EventId);
        log.RequestUrl.Should().Be("https://hooks.example.com/notify");
        log.RequestBody.Should().NotBeNullOrWhiteSpace();

        // Verify the request body is valid JSON containing expected payload fields
        var payload = JsonSerializer.Deserialize<JsonElement>(log.RequestBody);
        payload.GetProperty("eventType").GetString().Should().Be("product.created");
        payload.GetProperty("eventId").GetGuid().Should().Be(domainEvent.EventId);
    }

    #endregion

    #region Single Matching Subscription — DeliverWebhookCommand

    [Fact]
    public async Task DispatchAsync_SingleMatchingSubscription_ShouldPublishDeliverWebhookCommandWithCorrectParameters()
    {
        // Arrange
        var subscription = CreateSubscription(
            eventPatterns: "product.*",
            url: "https://hooks.example.com/notify");
        SetupActiveSubscriptions(subscription);

        var domainEvent = new ProductCreatedEvent(Guid.NewGuid(), "Widget", "widget");
        DeliverWebhookCommand? capturedCommand = null;

        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Callback<object, DeliveryOptions?>((cmd, _) => capturedCommand = cmd as DeliverWebhookCommand)
            .Returns(new ValueTask());

        // Act
        await _dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert
        capturedCommand.Should().NotBeNull();
        capturedCommand!.SubscriptionId.Should().Be(subscription.Id);
        capturedCommand.EventType.Should().Be("product.created");
        capturedCommand.EventId.Should().Be(domainEvent.EventId);
        capturedCommand.Url.Should().Be("https://hooks.example.com/notify");
        capturedCommand.Secret.Should().Be(subscription.Secret);
        capturedCommand.TimeoutSeconds.Should().Be(subscription.TimeoutSeconds);
        capturedCommand.MaxRetries.Should().Be(subscription.MaxRetries);
        capturedCommand.AttemptNumber.Should().Be(1);
        capturedCommand.CustomHeaders.Should().Be(subscription.CustomHeaders);
        capturedCommand.Payload.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Multiple Matching Subscriptions

    [Fact]
    public async Task DispatchAsync_MultipleMatchingSubscriptions_ShouldCreateDeliveryLogAndCommandForEach()
    {
        // Arrange — 3 subscriptions all matching "product.*"
        var sub1 = CreateSubscription(eventPatterns: "product.*", url: "https://hook1.example.com/a", name: "Hook 1");
        var sub2 = CreateSubscription(eventPatterns: "product.created", url: "https://hook2.example.com/b", name: "Hook 2");
        var sub3 = CreateSubscription(eventPatterns: "*", url: "https://hook3.example.com/c", name: "Hook 3");
        SetupActiveSubscriptions(sub1, sub2, sub3);

        var domainEvent = new ProductCreatedEvent(Guid.NewGuid(), "Widget", "widget");
        var publishedCommands = new List<DeliverWebhookCommand>();

        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Callback<object, DeliveryOptions?>((cmd, _) => publishedCommands.Add((DeliverWebhookCommand)cmd))
            .Returns(new ValueTask());

        // Act
        await _dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert — 3 delivery logs, 3 published commands
        _capturedDeliveryLogs.Should().HaveCount(3);
        publishedCommands.Should().HaveCount(3);

        // Each delivery log has a unique subscription ID
        _capturedDeliveryLogs.Select(l => l.WebhookSubscriptionId)
            .Should().BeEquivalentTo(new[] { sub1.Id, sub2.Id, sub3.Id });

        // Each command references the correct subscription
        publishedCommands.Select(c => c.SubscriptionId)
            .Should().BeEquivalentTo(new[] { sub1.Id, sub2.Id, sub3.Id });
    }

    #endregion

    #region Payload JSON Structure

    [Fact]
    public async Task DispatchAsync_PayloadJson_ShouldContainCorrectStructure()
    {
        // Arrange
        var subscription = CreateSubscription(eventPatterns: "product.*");
        SetupActiveSubscriptions(subscription);

        var domainEvent = new ProductCreatedEvent(Guid.NewGuid(), "Cool Product", "cool-product");

        // Act
        await _dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert
        _capturedDeliveryLogs.Should().HaveCount(1);
        var payloadJson = _capturedDeliveryLogs[0].RequestBody;
        payloadJson.Should().NotBeNullOrWhiteSpace();

        var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);

        // Required fields from WebhookPayload
        payload.GetProperty("eventType").GetString().Should().Be("product.created");
        payload.GetProperty("eventId").GetGuid().Should().Be(domainEvent.EventId);
        payload.GetProperty("timestamp").GetDateTimeOffset().Should().BeCloseTo(domainEvent.OccurredAt, TimeSpan.FromSeconds(5));
        payload.GetProperty("apiVersion").GetString().Should().Be("2026-02-26");

        // data field should contain the domain event payload
        var data = payload.GetProperty("data");
        data.GetProperty("productId").GetGuid().Should().Be(domainEvent.ProductId);
        data.GetProperty("name").GetString().Should().Be("Cool Product");
        data.GetProperty("slug").GetString().Should().Be("cool-product");
    }

    #endregion

    #region Save-Before-Publish Ordering

    [Fact]
    public async Task DispatchAsync_SaveChangesAsync_ShouldBeCalledBeforePublishAsync()
    {
        // Arrange
        var subscription = CreateSubscription(eventPatterns: "product.*");
        SetupActiveSubscriptions(subscription);

        var domainEvent = new ProductCreatedEvent(Guid.NewGuid(), "Widget", "widget");

        var callOrder = new List<string>();

        _dbContextMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => callOrder.Add("SaveChangesAsync"))
            .ReturnsAsync(1);

        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Callback<object, DeliveryOptions?>((_, __) => callOrder.Add("PublishAsync"))
            .Returns(new ValueTask());

        // Act
        await _dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert — SaveChangesAsync must appear before any PublishAsync calls
        callOrder.Should().ContainInOrder("SaveChangesAsync", "PublishAsync");
        callOrder.IndexOf("SaveChangesAsync").Should().BeLessThan(callOrder.IndexOf("PublishAsync"),
            "delivery logs must be persisted before publishing commands to avoid race condition");
    }

    #endregion

    #region Mixed Matching — Partial Pattern Match

    [Fact]
    public async Task DispatchAsync_MixedSubscriptions_ShouldOnlyDispatchToMatchingOnes()
    {
        // Arrange — 3 subscriptions, but only 2 match "product.created"
        var matchingSub1 = CreateSubscription(eventPatterns: "product.*", url: "https://hook1.example.com/a", name: "Product Hook");
        var nonMatchingSub = CreateSubscription(eventPatterns: "order.*", url: "https://hook2.example.com/b", name: "Order Hook");
        var matchingSub2 = CreateSubscription(eventPatterns: "product.created", url: "https://hook3.example.com/c", name: "Specific Hook");
        SetupActiveSubscriptions(matchingSub1, nonMatchingSub, matchingSub2);

        var domainEvent = new ProductCreatedEvent(Guid.NewGuid(), "Widget", "widget");
        var publishedCommands = new List<DeliverWebhookCommand>();

        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Callback<object, DeliveryOptions?>((cmd, _) => publishedCommands.Add((DeliverWebhookCommand)cmd))
            .Returns(new ValueTask());

        // Act
        await _dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert — only 2 delivery logs and 2 commands (not 3)
        _capturedDeliveryLogs.Should().HaveCount(2);
        publishedCommands.Should().HaveCount(2);

        // The non-matching subscription should NOT appear
        _capturedDeliveryLogs.Select(l => l.WebhookSubscriptionId)
            .Should().BeEquivalentTo(new[] { matchingSub1.Id, matchingSub2.Id });

        publishedCommands.Select(c => c.SubscriptionId)
            .Should().BeEquivalentTo(new[] { matchingSub1.Id, matchingSub2.Id });

        // The non-matching subscription should NOT have been dispatched to
        _capturedDeliveryLogs.Select(l => l.WebhookSubscriptionId)
            .Should().NotContain(nonMatchingSub.Id);
    }

    #endregion

    #region Delivery Log DeliveryLogId Links to Command

    [Fact]
    public async Task DispatchAsync_DeliveryLogId_ShouldMatchBetweenLogAndCommand()
    {
        // Arrange
        var subscription = CreateSubscription(eventPatterns: "product.*");
        SetupActiveSubscriptions(subscription);

        var domainEvent = new ProductCreatedEvent(Guid.NewGuid(), "Widget", "widget");
        DeliverWebhookCommand? capturedCommand = null;

        _messageBusMock
            .Setup(x => x.PublishAsync(It.IsAny<object>(), It.IsAny<DeliveryOptions?>()))
            .Callback<object, DeliveryOptions?>((cmd, _) => capturedCommand = cmd as DeliverWebhookCommand)
            .Returns(new ValueTask());

        // Act
        await _dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert — the delivery log ID in the command matches the created log's ID
        _capturedDeliveryLogs.Should().HaveCount(1);
        capturedCommand.Should().NotBeNull();

        capturedCommand!.DeliveryLogId.Should().Be(_capturedDeliveryLogs[0].Id);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// A domain event type that is NOT registered in the WebhookEventTypeRegistry.
    /// Used to verify unknown events are skipped.
    /// </summary>
    private sealed record UnregisteredTestEvent() : DomainEvent;

    #endregion
}
