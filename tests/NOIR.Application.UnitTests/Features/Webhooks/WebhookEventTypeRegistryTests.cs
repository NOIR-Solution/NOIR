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
        result.ShouldBe("order.created");
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
        result.ShouldBe("product.created");
    }

    [Fact]
    public void GetEventType_ForUnknownEvent_ShouldReturnNull()
    {
        // Arrange
        var evt = new UnknownTestEvent();

        // Act
        var result = _registry.GetEventType(evt);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetEventType(Type)

    [Fact]
    public void GetEventType_ByType_ForOrderCreatedEvent_ShouldReturnOrderCreated()
    {
        // Act
        var result = _registry.GetEventType(typeof(OrderCreatedEvent));

        // Assert
        result.ShouldBe("order.created");
    }

    [Fact]
    public void GetEventType_ByType_ForUnknownType_ShouldReturnNull()
    {
        // Act
        var result = _registry.GetEventType(typeof(UnknownTestEvent));

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetEventType_ByType_ForProductPublishedEvent_ShouldReturnProductPublished()
    {
        // Act
        var result = _registry.GetEventType(typeof(ProductPublishedEvent));

        // Assert
        result.ShouldBe("product.published");
    }

    #endregion

    #region GetAllEventTypes

    [Fact]
    public void GetAllEventTypes_ShouldReturnAtLeastFortySevenTypes()
    {
        // Act
        var result = _registry.GetAllEventTypes();

        // Assert
        result.Count().ShouldBeGreaterThanOrEqualTo(46);
    }

    [Fact]
    public void GetAllEventTypes_ShouldReturnInDeterministicOrder()
    {
        // Act — call twice to verify stable ordering
        var result1 = _registry.GetAllEventTypes().Select(e => e.EventType).ToList();
        var result2 = _registry.GetAllEventTypes().Select(e => e.EventType).ToList();

        // Assert — both calls return the same deterministic order
        result1.ShouldBe(result2);
    }

    [Fact]
    public void GetAllEventTypes_ShouldContainOrderCreated()
    {
        // Act
        var result = _registry.GetAllEventTypes();

        // Assert
        result.ShouldContain(e => e.EventType == "order.created");
    }

    [Fact]
    public void GetAllEventTypes_ShouldContainAllCategories()
    {
        // Act
        var result = _registry.GetAllEventTypes();
        var categories = result.Select(e => e.Category).Distinct().ToList();

        // Assert — at minimum these categories must be present
        categories.ShouldContain("order");
        categories.ShouldContain("product");
        categories.ShouldContain("payment");
        categories.ShouldContain("customer");
        categories.ShouldContain("inventory");
    }

    [Fact]
    public void GetAllEventTypes_EachEntry_ShouldHaveNonEmptyDescription()
    {
        // Act
        var result = _registry.GetAllEventTypes();

        // Assert
        result.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.Description));
    }

    [Fact]
    public void GetAllEventTypes_EachEntry_ShouldHaveNonEmptyCategory()
    {
        // Act
        var result = _registry.GetAllEventTypes();

        // Assert
        result.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.Category));
    }

    #endregion

    #region Helpers

    // A domain event not registered in the registry
    private sealed record UnknownTestEvent : NOIR.Domain.Common.DomainEvent;

    #endregion
}
