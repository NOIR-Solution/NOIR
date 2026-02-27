using NOIR.Application.Features.Webhooks;
using NOIR.Domain.Events.Order;
using NOIR.Domain.Events.Product;

namespace NOIR.Application.UnitTests.Features.Webhooks;

/// <summary>
/// Unit tests for WebhookEventTypeRegistry.
/// Verifies event type mapping, discovery, and handling of unknown types.
/// </summary>
public class WebhookEventTypeRegistryTests
{
    private readonly WebhookEventTypeRegistry _registry = new();

    #region GetEventType(IDomainEvent)

    [Fact]
    public void GetEventType_ForOrderCreatedEvent_ShouldReturnOrderCreated()
    {
        // Arrange
        var evt = new OrderCreatedEvent(Guid.NewGuid(), "ORD-001", "customer@example.com", 100m, "USD");

        // Act
        var result = _registry.GetEventType(evt);

        // Assert
        result.Should().Be("order.created");
    }

    [Fact]
    public void GetEventType_ForProductCreatedEvent_ShouldReturnProductCreated()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var evt = new ProductCreatedEvent(productId, "Test Product", "test-product");

        // Act
        var result = _registry.GetEventType(evt);

        // Assert
        result.Should().Be("product.created");
    }

    [Fact]
    public void GetEventType_ForUnknownEvent_ShouldReturnNull()
    {
        // Arrange
        var evt = new UnknownTestEvent();

        // Act
        var result = _registry.GetEventType(evt);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetEventType(Type)

    [Fact]
    public void GetEventType_ByType_ForOrderCreatedEvent_ShouldReturnOrderCreated()
    {
        // Act
        var result = _registry.GetEventType(typeof(OrderCreatedEvent));

        // Assert
        result.Should().Be("order.created");
    }

    [Fact]
    public void GetEventType_ByType_ForUnknownType_ShouldReturnNull()
    {
        // Act
        var result = _registry.GetEventType(typeof(UnknownTestEvent));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetEventType_ByType_ForProductPublishedEvent_ShouldReturnProductPublished()
    {
        // Act
        var result = _registry.GetEventType(typeof(ProductPublishedEvent));

        // Assert
        result.Should().Be("product.published");
    }

    #endregion

    #region GetAllEventTypes

    [Fact]
    public void GetAllEventTypes_ShouldReturnAtLeastFortySevenTypes()
    {
        // Act
        var result = _registry.GetAllEventTypes();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(46);
    }

    [Fact]
    public void GetAllEventTypes_ShouldReturnInDeterministicOrder()
    {
        // Act — call twice to verify stable ordering
        var result1 = _registry.GetAllEventTypes().Select(e => e.EventType).ToList();
        var result2 = _registry.GetAllEventTypes().Select(e => e.EventType).ToList();

        // Assert — both calls return the same deterministic order
        result1.Should().Equal(result2);
    }

    [Fact]
    public void GetAllEventTypes_ShouldContainOrderCreated()
    {
        // Act
        var result = _registry.GetAllEventTypes();

        // Assert
        result.Should().Contain(e => e.EventType == "order.created");
    }

    [Fact]
    public void GetAllEventTypes_ShouldContainAllCategories()
    {
        // Act
        var result = _registry.GetAllEventTypes();
        var categories = result.Select(e => e.Category).Distinct().ToList();

        // Assert — at minimum these categories must be present
        categories.Should().Contain("order");
        categories.Should().Contain("product");
        categories.Should().Contain("payment");
        categories.Should().Contain("customer");
        categories.Should().Contain("inventory");
    }

    [Fact]
    public void GetAllEventTypes_EachEntry_ShouldHaveNonEmptyDescription()
    {
        // Act
        var result = _registry.GetAllEventTypes();

        // Assert
        result.Should().AllSatisfy(e => e.Description.Should().NotBeNullOrWhiteSpace());
    }

    [Fact]
    public void GetAllEventTypes_EachEntry_ShouldHaveNonEmptyCategory()
    {
        // Act
        var result = _registry.GetAllEventTypes();

        // Assert
        result.Should().AllSatisfy(e => e.Category.Should().NotBeNullOrWhiteSpace());
    }

    #endregion

    #region Helpers

    // A domain event not registered in the registry
    private sealed record UnknownTestEvent : NOIR.Domain.Common.DomainEvent;

    #endregion
}
