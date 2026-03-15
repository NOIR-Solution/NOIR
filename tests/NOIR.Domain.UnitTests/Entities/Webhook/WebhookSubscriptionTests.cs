using NOIR.Domain.Entities.Webhook;
using NOIR.Domain.Events.Webhook;

namespace NOIR.Domain.UnitTests.Entities.Webhook;

/// <summary>
/// Unit tests verifying WebhookSubscription aggregate root behavior:
/// creation, state transitions, event pattern matching, secret rotation, and delivery recording.
/// </summary>
public class WebhookSubscriptionTests
{
    private const string TestTenantId = "test-tenant";

    private static WebhookSubscription CreateTestSubscription(
        string name = "My Webhook",
        string url = "https://example.com/webhook",
        string eventPatterns = "order.*",
        string? description = null,
        string? customHeaders = null,
        int maxRetries = 5,
        int timeoutSeconds = 30,
        string? tenantId = TestTenantId)
    {
        return WebhookSubscription.Create(name, url, eventPatterns, description, customHeaders, maxRetries, timeoutSeconds, tenantId);
    }

    #region Create

    [Fact]
    public void Create_ShouldSetAllProperties()
    {
        // Act
        var subscription = WebhookSubscription.Create(
            name: "Order Notifications",
            url: "https://api.example.com/hooks",
            eventPatterns: "order.*,payment.*",
            description: "Handles order and payment events",
            customHeaders: "{\"X-API-Key\":\"secret\"}",
            maxRetries: 3,
            timeoutSeconds: 15,
            tenantId: TestTenantId);

        // Assert
        subscription.Name.ShouldBe("Order Notifications");
        subscription.Url.ShouldBe("https://api.example.com/hooks");
        subscription.EventPatterns.ShouldBe("order.*,payment.*");
        subscription.Description.ShouldBe("Handles order and payment events");
        subscription.CustomHeaders.ShouldBe("{\"X-API-Key\":\"secret\"}");
        subscription.MaxRetries.ShouldBe(3);
        subscription.TimeoutSeconds.ShouldBe(15);
        subscription.IsActive.ShouldBeTrue();
        subscription.Status.ShouldBe(WebhookSubscriptionStatus.Active);
        subscription.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldGenerateSixtyFourCharHexSecret()
    {
        // Act
        var subscription = CreateTestSubscription();

        // Assert
        subscription.Secret.ShouldNotBeNullOrEmpty();
        subscription.Secret.Length.ShouldBe(64);
        subscription.Secret.ShouldMatch("^[0-9a-f]{64}$");
    }

    [Fact]
    public void Create_ShouldSetStatusActive()
    {
        // Act
        var subscription = CreateTestSubscription();

        // Assert
        subscription.Status.ShouldBe(WebhookSubscriptionStatus.Active);
        subscription.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldRaiseWebhookSubscriptionCreatedEvent()
    {
        // Act
        var subscription = CreateTestSubscription(name: "My Webhook", url: "https://example.com/webhook");

        // Assert
        subscription.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<WebhookSubscriptionCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseCreatedEventWithCorrectProperties()
    {
        // Act
        var subscription = CreateTestSubscription(name: "My Webhook", url: "https://example.com/webhook");

        // Assert
        var evt = subscription.DomainEvents.OfType<WebhookSubscriptionCreatedEvent>().Single();
        evt.SubscriptionId.ShouldBe(subscription.Id);
        evt.Name.ShouldBe("My Webhook");
        evt.Url.ShouldBe("https://example.com/webhook");
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => WebhookSubscription.Create(null!, "https://example.com/webhook", "order.*");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => WebhookSubscription.Create("", "https://example.com/webhook", "order.*");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldThrowArgumentException()
    {
        // Act
        var act = () => WebhookSubscription.Create("   ", "https://example.com/webhook", "order.*");

        // Assert
        Should.Throw<ArgumentException>(act);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_ShouldUpdateAllProperties()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        subscription.Update(
            name: "Updated Name",
            url: "https://updated.example.com/webhook",
            eventPatterns: "product.*,review.*",
            description: "Updated description",
            customHeaders: "{\"Authorization\":\"Bearer token\"}",
            maxRetries: 3,
            timeoutSeconds: 45);

        // Assert
        subscription.Name.ShouldBe("Updated Name");
        subscription.Url.ShouldBe("https://updated.example.com/webhook");
        subscription.EventPatterns.ShouldBe("product.*,review.*");
        subscription.Description.ShouldBe("Updated description");
        subscription.CustomHeaders.ShouldBe("{\"Authorization\":\"Bearer token\"}");
        subscription.MaxRetries.ShouldBe(3);
        subscription.TimeoutSeconds.ShouldBe(45);
    }

    [Fact]
    public void Update_WithNullDescription_ShouldSetDescriptionToNull()
    {
        // Arrange
        var subscription = CreateTestSubscription(description: "Some desc");

        // Act
        subscription.Update("Updated", "https://example.com/webhook", "order.*", null, null, 5, 30);

        // Assert
        subscription.Description.ShouldBeNull();
    }

    #endregion

    #region Activate

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Deactivate();
        subscription.ClearDomainEvents();

        // Act
        subscription.Activate();

        // Assert
        subscription.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Activate_ShouldSetStatusActive()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Deactivate();
        subscription.ClearDomainEvents();

        // Act
        subscription.Activate();

        // Assert
        subscription.Status.ShouldBe(WebhookSubscriptionStatus.Active);
    }

    [Fact]
    public void Activate_ShouldRaiseWebhookSubscriptionActivatedEvent()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.Deactivate();
        subscription.ClearDomainEvents();

        // Act
        subscription.Activate();

        // Assert
        subscription.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<WebhookSubscriptionActivatedEvent>();
    }

    [Fact]
    public void Activate_ShouldRaiseActivatedEventWithCorrectProperties()
    {
        // Arrange
        var subscription = CreateTestSubscription(name: "My Webhook");
        subscription.Deactivate();
        subscription.ClearDomainEvents();

        // Act
        subscription.Activate();

        // Assert
        var evt = subscription.DomainEvents.OfType<WebhookSubscriptionActivatedEvent>().Single();
        evt.SubscriptionId.ShouldBe(subscription.Id);
        evt.Name.ShouldBe("My Webhook");
    }

    #endregion

    #region Deactivate

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.ClearDomainEvents();

        // Act
        subscription.Deactivate();

        // Assert
        subscription.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_ShouldSetStatusInactive()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.ClearDomainEvents();

        // Act
        subscription.Deactivate();

        // Assert
        subscription.Status.ShouldBe(WebhookSubscriptionStatus.Inactive);
    }

    [Fact]
    public void Deactivate_ShouldRaiseWebhookSubscriptionDeactivatedEvent()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.ClearDomainEvents();

        // Act
        subscription.Deactivate();

        // Assert
        subscription.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<WebhookSubscriptionDeactivatedEvent>();
    }

    [Fact]
    public void Deactivate_ShouldRaiseDeactivatedEventWithCorrectProperties()
    {
        // Arrange
        var subscription = CreateTestSubscription(name: "My Webhook");
        subscription.ClearDomainEvents();

        // Act
        subscription.Deactivate();

        // Assert
        var evt = subscription.DomainEvents.OfType<WebhookSubscriptionDeactivatedEvent>().Single();
        evt.SubscriptionId.ShouldBe(subscription.Id);
        evt.Name.ShouldBe("My Webhook");
    }

    #endregion

    #region Suspend

    [Fact]
    public void Suspend_ShouldSetIsActiveFalse()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        subscription.Suspend();

        // Assert
        subscription.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Suspend_ShouldSetStatusSuspended()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        subscription.Suspend();

        // Assert
        subscription.Status.ShouldBe(WebhookSubscriptionStatus.Suspended);
    }

    [Fact]
    public void Suspend_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.ClearDomainEvents();

        // Act
        subscription.Suspend();

        // Assert
        subscription.DomainEvents.ShouldBeEmpty();
    }

    #endregion

    #region MatchesEvent

    [Fact]
    public void MatchesEvent_WithWildcardPattern_WhenEventMatchesPrefix_ShouldReturnTrue()
    {
        // Arrange
        var subscription = CreateTestSubscription(eventPatterns: "order.*");

        // Act
        var result = subscription.MatchesEvent("order.created");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_WithWildcardPattern_WhenEventDoesNotMatchPrefix_ShouldReturnFalse()
    {
        // Arrange
        var subscription = CreateTestSubscription(eventPatterns: "payment.*");

        // Act
        var result = subscription.MatchesEvent("order.created");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void MatchesEvent_WithGlobalWildcard_ShouldReturnTrue()
    {
        // Arrange
        var subscription = CreateTestSubscription(eventPatterns: "*");

        // Act
        var result = subscription.MatchesEvent("order.created");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_WithExactPattern_WhenEventMatches_ShouldReturnTrue()
    {
        // Arrange
        var subscription = CreateTestSubscription(eventPatterns: "order.created");

        // Act
        var result = subscription.MatchesEvent("order.created");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_WithExactPattern_WhenEventDoesNotMatch_ShouldReturnFalse()
    {
        // Arrange
        var subscription = CreateTestSubscription(eventPatterns: "order.created");

        // Act
        var result = subscription.MatchesEvent("order.cancelled");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void MatchesEvent_WithEmptyEventType_ShouldReturnFalse()
    {
        // Arrange
        var subscription = CreateTestSubscription(eventPatterns: "order.*");

        // Act
        var result = subscription.MatchesEvent("");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void MatchesEvent_WithMultiplePatterns_WhenOneMatches_ShouldReturnTrue()
    {
        // Arrange
        var subscription = CreateTestSubscription(eventPatterns: "product.*,payment.*");

        // Act
        var result = subscription.MatchesEvent("payment.succeeded");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void MatchesEvent_WithMultiplePatterns_WhenNoneMatch_ShouldReturnFalse()
    {
        // Arrange
        var subscription = CreateTestSubscription(eventPatterns: "product.*,payment.*");

        // Act
        var result = subscription.MatchesEvent("order.created");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region RotateSecret

    [Fact]
    public void RotateSecret_ShouldGenerateNewSecret()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var originalSecret = subscription.Secret;

        // Act
        var newSecret = subscription.RotateSecret();

        // Assert
        newSecret.ShouldNotBe(originalSecret);
        subscription.Secret.ShouldNotBe(originalSecret);
    }

    [Fact]
    public void RotateSecret_ShouldReturnSixtyFourCharHexSecret()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var newSecret = subscription.RotateSecret();

        // Assert
        newSecret.Length.ShouldBe(64);
        newSecret.ShouldMatch("^[0-9a-f]{64}$");
    }

    [Fact]
    public void RotateSecret_ShouldUpdateSecretProperty()
    {
        // Arrange
        var subscription = CreateTestSubscription();

        // Act
        var newSecret = subscription.RotateSecret();

        // Assert
        subscription.Secret.ShouldBe(newSecret);
    }

    #endregion

    #region RecordDelivery

    [Fact]
    public void RecordDelivery_ShouldSetLastDeliveryAt()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        // Act
        subscription.RecordDelivery();

        // Assert
        subscription.LastDeliveryAt.ShouldNotBeNull();
        subscription.LastDeliveryAt!.Value.ShouldBeGreaterThan(before);
        subscription.LastDeliveryAt!.Value.ShouldBeLessThan(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void RecordDelivery_CalledMultipleTimes_ShouldUpdateLastDeliveryAt()
    {
        // Arrange
        var subscription = CreateTestSubscription();
        subscription.RecordDelivery();
        var firstDeliveryAt = subscription.LastDeliveryAt;

        // Act
        subscription.RecordDelivery();

        // Assert
        subscription.LastDeliveryAt!.Value.ShouldBeGreaterThanOrEqualTo(firstDeliveryAt!.Value);
    }

    #endregion
}
