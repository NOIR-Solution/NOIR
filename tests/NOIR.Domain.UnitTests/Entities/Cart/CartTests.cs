using NOIR.Domain.Entities.Cart;
using NOIR.Domain.Events.Cart;

namespace NOIR.Domain.UnitTests.Entities.Cart;

/// <summary>
/// Unit tests for the Cart aggregate root and CartItem entity.
/// Tests factory methods, item management, status transitions, merge operations,
/// computed properties, and domain event raising.
/// </summary>
public class CartTests
{
    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "user-123";
    private const string TestSessionId = "session-abc-123";
    private const string TestCurrency = "VND";

    private static readonly Guid ProductId1 = Guid.NewGuid();
    private static readonly Guid VariantId1 = Guid.NewGuid();
    private static readonly Guid ProductId2 = Guid.NewGuid();
    private static readonly Guid VariantId2 = Guid.NewGuid();

    #region Helper Methods

    private static Domain.Entities.Cart.Cart CreateActiveUserCart(string? tenantId = TestTenantId)
    {
        return Domain.Entities.Cart.Cart.CreateForUser(TestUserId, TestCurrency, tenantId);
    }

    private static Domain.Entities.Cart.Cart CreateActiveGuestCart(string? tenantId = TestTenantId)
    {
        return Domain.Entities.Cart.Cart.CreateForGuest(TestSessionId, TestCurrency, tenantId);
    }

    private static Domain.Entities.Cart.Cart CreateCartWithItems(int itemCount = 2)
    {
        var cart = CreateActiveUserCart();
        for (var i = 0; i < itemCount; i++)
        {
            cart.AddItem(
                Guid.NewGuid(),
                Guid.NewGuid(),
                $"Product {i + 1}",
                $"Variant {i + 1}",
                100.00m * (i + 1),
                i + 1);
        }
        return cart;
    }

    #endregion

    #region CreateForUser Tests

    [Fact]
    public void CreateForUser_WithValidParameters_ShouldCreateCartWithCorrectProperties()
    {
        // Arrange & Act
        var cart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId, TestCurrency, TestTenantId);

        // Assert
        cart.ShouldNotBeNull();
        cart.Id.ShouldNotBe(Guid.Empty);
        cart.UserId.ShouldBe(TestUserId);
        cart.SessionId.ShouldBeNull();
        cart.Status.ShouldBe(CartStatus.Active);
        cart.Currency.ShouldBe(TestCurrency);
        cart.TenantId.ShouldBe(TestTenantId);
        cart.ExpiresAt.ShouldBeNull();
        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void CreateForUser_ShouldDefaultToActiveStatus()
    {
        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId);

        // Assert
        cart.Status.ShouldBe(CartStatus.Active);
    }

    [Fact]
    public void CreateForUser_ShouldDefaultCurrencyToVND()
    {
        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId);

        // Assert
        cart.Currency.ShouldBe("VND");
    }

    [Fact]
    public void CreateForUser_ShouldSetLastActivityAt()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId);

        // Assert
        cart.LastActivityAt.ShouldBeGreaterThanOrEqualTo(before);
        cart.LastActivityAt.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void CreateForUser_ShouldNotBeGuest()
    {
        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId);

        // Assert
        cart.IsGuest.ShouldBeFalse();
    }

    [Fact]
    public void CreateForUser_ShouldRaiseCartCreatedEvent()
    {
        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId);

        // Assert
        cart.DomainEvents.ShouldHaveSingleItem();
        var domainEvent = cart.DomainEvents.First();
        domainEvent.ShouldBeOfType<CartCreatedEvent>();
        var createdEvent = (CartCreatedEvent)domainEvent;
        createdEvent.CartId.ShouldBe(cart.Id);
        createdEvent.UserId.ShouldBe(TestUserId);
        createdEvent.SessionId.ShouldBeNull();
    }

    [Fact]
    public void CreateForUser_ShouldHaveEmptyItems()
    {
        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId);

        // Assert
        cart.IsEmpty.ShouldBeTrue();
        cart.ItemCount.ShouldBe(0);
        cart.Subtotal.ShouldBe(0m);
    }

    #endregion

    #region CreateForGuest Tests

    [Fact]
    public void CreateForGuest_WithValidParameters_ShouldCreateCartWithCorrectProperties()
    {
        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForGuest(TestSessionId, TestCurrency, TestTenantId);

        // Assert
        cart.ShouldNotBeNull();
        cart.Id.ShouldNotBe(Guid.Empty);
        cart.UserId.ShouldBeNull();
        cart.SessionId.ShouldBe(TestSessionId);
        cart.Status.ShouldBe(CartStatus.Active);
        cart.Currency.ShouldBe(TestCurrency);
        cart.TenantId.ShouldBe(TestTenantId);
        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void CreateForGuest_ShouldSetExpiresAt()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForGuest(TestSessionId);

        // Assert
        cart.ExpiresAt.ShouldNotBeNull();
        cart.ExpiresAt!.Value.ShouldBeGreaterThan(before.AddDays(29));
        cart.ExpiresAt!.Value.ShouldBeLessThanOrEqualTo(DateTimeOffset.UtcNow.AddDays(30));
    }

    [Fact]
    public void CreateForGuest_ShouldBeGuest()
    {
        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForGuest(TestSessionId);

        // Assert
        cart.IsGuest.ShouldBeTrue();
    }

    [Fact]
    public void CreateForGuest_ShouldRaiseCartCreatedEvent()
    {
        // Act
        var cart = Domain.Entities.Cart.Cart.CreateForGuest(TestSessionId);

        // Assert
        cart.DomainEvents.ShouldHaveSingleItem();
        var createdEvent = cart.DomainEvents.OfType<CartCreatedEvent>().Single();
        createdEvent.CartId.ShouldBe(cart.Id);
        createdEvent.UserId.ShouldBeNull();
        createdEvent.SessionId.ShouldBe(TestSessionId);
    }

    #endregion

    #region AddItem Tests

    [Fact]
    public void AddItem_ToActiveCart_ShouldAddItemSuccessfully()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act
        var item = cart.AddItem(ProductId1, VariantId1, "Test Product", "Size: M", 50000m, 2, "http://img.jpg");

        // Assert
        item.ShouldNotBeNull();
        cart.Items.Count().ShouldBe(1);
        item.ProductId.ShouldBe(ProductId1);
        item.ProductVariantId.ShouldBe(VariantId1);
        item.ProductName.ShouldBe("Test Product");
        item.VariantName.ShouldBe("Size: M");
        item.UnitPrice.ShouldBe(50000m);
        item.Quantity.ShouldBe(2);
        item.ImageUrl.ShouldBe("http://img.jpg");
    }

    [Fact]
    public void AddItem_WithDefaultQuantity_ShouldDefaultToOne()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m);

        // Assert
        item.Quantity.ShouldBe(1);
    }

    [Fact]
    public void AddItem_DuplicateProductVariant_ShouldIncrementQuantity()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 50000m, 2);

        // Act
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 50000m, 3);

        // Assert
        cart.Items.Count().ShouldBe(1);
        item.Quantity.ShouldBe(5); // 2 + 3
    }

    [Fact]
    public void AddItem_SameProductDifferentVariant_ShouldAddSeparateItem()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var variantId2 = Guid.NewGuid();
        cart.AddItem(ProductId1, VariantId1, "Product", "Size: S", 50000m, 1);

        // Act
        cart.AddItem(ProductId1, variantId2, "Product", "Size: M", 55000m, 1);

        // Assert
        cart.Items.Count().ShouldBe(2);
    }

    [Fact]
    public void AddItem_MultipleProducts_ShouldAddAllItems()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act
        cart.AddItem(ProductId1, VariantId1, "Product 1", "Variant 1", 10000m, 1);
        cart.AddItem(ProductId2, VariantId2, "Product 2", "Variant 2", 20000m, 2);

        // Assert
        cart.Items.Count().ShouldBe(2);
        cart.ItemCount.ShouldBe(3); // 1 + 2
    }

    [Fact]
    public void AddItem_WithZeroQuantity_ShouldThrow()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act
        var act = () => cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m, 0);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Quantity must be greater than zero");
    }

    [Fact]
    public void AddItem_WithNegativeQuantity_ShouldThrow()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act
        var act = () => cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m, -1);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Quantity must be greater than zero");
    }

    [Theory]
    [InlineData(CartStatus.Abandoned)]
    [InlineData(CartStatus.Converted)]
    [InlineData(CartStatus.Expired)]
    [InlineData(CartStatus.Merged)]
    public void AddItem_ToInactiveCart_ShouldThrow(CartStatus inactiveStatus)
    {
        // Arrange
        var cart = CreateActiveUserCart();
        // Transition to inactive status
        switch (inactiveStatus)
        {
            case CartStatus.Abandoned:
                cart.MarkAsAbandoned();
                break;
            case CartStatus.Converted:
                cart.MarkAsConverted(Guid.NewGuid());
                break;
            case CartStatus.Expired:
                cart.MarkAsExpired();
                break;
            case CartStatus.Merged:
                cart.MarkAsMerged(Guid.NewGuid(), TestUserId, 0);
                break;
        }

        // Act
        var act = () => cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot add items to an inactive cart");
    }

    [Fact]
    public void AddItem_ShouldRaiseCartItemAddedEvent()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.ClearDomainEvents(); // Clear CartCreatedEvent

        // Act
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m, 3);

        // Assert
        cart.DomainEvents.ShouldHaveSingleItem();
        var addedEvent = cart.DomainEvents.OfType<CartItemAddedEvent>().Single();
        addedEvent.CartId.ShouldBe(cart.Id);
        addedEvent.CartItemId.ShouldBe(item.Id);
        addedEvent.ProductId.ShouldBe(ProductId1);
        addedEvent.ProductVariantId.ShouldBe(VariantId1);
        addedEvent.Quantity.ShouldBe(3);
    }

    [Fact]
    public void AddItem_DuplicateProductVariant_ShouldNotRaiseItemAddedEvent()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 50000m, 1);
        cart.ClearDomainEvents();

        // Act
        cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 50000m, 2);

        // Assert - no CartItemAddedEvent for duplicate, just quantity update
        cart.DomainEvents.OfType<CartItemAddedEvent>().ShouldBeEmpty();
    }

    [Fact]
    public void AddItem_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var initialActivity = cart.LastActivityAt;

        // Act
        // Small delay to ensure timestamp difference
        cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m);

        // Assert
        cart.LastActivityAt.ShouldBeGreaterThanOrEqualTo(initialActivity);
    }

    #endregion

    #region UpdateItemQuantity Tests

    [Fact]
    public void UpdateItemQuantity_WithValidQuantity_ShouldUpdateSuccessfully()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m, 2);

        // Act
        cart.UpdateItemQuantity(item.Id, 5);

        // Assert
        item.Quantity.ShouldBe(5);
    }

    [Fact]
    public void UpdateItemQuantity_WithZeroQuantity_ShouldRemoveItem()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m, 2);

        // Act
        cart.UpdateItemQuantity(item.Id, 0);

        // Assert
        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateItemQuantity_WithNegativeQuantity_ShouldRemoveItem()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m, 2);

        // Act
        cart.UpdateItemQuantity(item.Id, -1);

        // Assert
        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateItemQuantity_NonExistentItem_ShouldThrow()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act
        var act = () => cart.UpdateItemQuantity(Guid.NewGuid(), 5);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cart item not found");
    }

    [Fact]
    public void UpdateItemQuantity_OnInactiveCart_ShouldThrow()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m, 2);
        cart.MarkAsAbandoned();

        // Act
        var act = () => cart.UpdateItemQuantity(item.Id, 3);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot update items in an inactive cart");
    }

    [Fact]
    public void UpdateItemQuantity_ShouldRaiseCartItemQuantityUpdatedEvent()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m, 2);
        cart.ClearDomainEvents();

        // Act
        cart.UpdateItemQuantity(item.Id, 5);

        // Assert
        var updatedEvent = cart.DomainEvents.OfType<CartItemQuantityUpdatedEvent>().Single();
        updatedEvent.CartId.ShouldBe(cart.Id);
        updatedEvent.CartItemId.ShouldBe(item.Id);
        updatedEvent.OldQuantity.ShouldBe(2);
        updatedEvent.NewQuantity.ShouldBe(5);
    }

    #endregion

    #region RemoveItem Tests

    [Fact]
    public void RemoveItem_ExistingItem_ShouldRemoveSuccessfully()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m);

        // Act
        cart.RemoveItem(item.Id);

        // Assert
        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveItem_NonExistentItem_ShouldNotThrow()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act
        var act = () => cart.RemoveItem(Guid.NewGuid());

        // Assert
        act.ShouldNotThrow();
    }

    [Fact]
    public void RemoveItem_OnInactiveCart_ShouldThrow()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m);
        cart.MarkAsConverted(Guid.NewGuid());

        // Act
        var act = () => cart.RemoveItem(item.Id);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot remove items from an inactive cart");
    }

    [Fact]
    public void RemoveItem_ShouldRaiseCartItemRemovedEvent()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var item = cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m);
        cart.ClearDomainEvents();

        // Act
        cart.RemoveItem(item.Id);

        // Assert
        var removedEvent = cart.DomainEvents.OfType<CartItemRemovedEvent>().Single();
        removedEvent.CartId.ShouldBe(cart.Id);
        removedEvent.CartItemId.ShouldBe(item.Id);
        removedEvent.ProductId.ShouldBe(ProductId1);
        removedEvent.ProductVariantId.ShouldBe(VariantId1);
    }

    [Fact]
    public void RemoveItem_OneOfMultiple_ShouldOnlyRemoveSpecifiedItem()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        var item1 = cart.AddItem(ProductId1, VariantId1, "Product 1", "Variant 1", 10000m);
        var item2 = cart.AddItem(ProductId2, VariantId2, "Product 2", "Variant 2", 20000m);

        // Act
        cart.RemoveItem(item1.Id);

        // Assert
        cart.Items.Count().ShouldBe(1);
        cart.Items.ShouldContain(i => i.Id == item2.Id);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ActiveCartWithItems_ShouldRemoveAllItems()
    {
        // Arrange
        var cart = CreateCartWithItems(3);

        // Act
        cart.Clear();

        // Assert
        cart.Items.ShouldBeEmpty();
        cart.IsEmpty.ShouldBeTrue();
        cart.ItemCount.ShouldBe(0);
        cart.Subtotal.ShouldBe(0m);
    }

    [Fact]
    public void Clear_EmptyCart_ShouldNotThrow()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act
        var act = () => cart.Clear();

        // Assert
        act.ShouldNotThrow();
        cart.Items.ShouldBeEmpty();
    }

    [Fact]
    public void Clear_OnInactiveCart_ShouldThrow()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsAbandoned();

        // Act
        var act = () => cart.Clear();

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot clear an inactive cart");
    }

    #endregion

    #region MergeFrom Tests

    [Fact]
    public void MergeFrom_ShouldCopyAllItemsFromSourceCart()
    {
        // Arrange
        var targetCart = CreateActiveUserCart();
        var sourceCart = CreateActiveGuestCart();
        sourceCart.AddItem(ProductId1, VariantId1, "Product 1", "Variant 1", 10000m, 2);
        sourceCart.AddItem(ProductId2, VariantId2, "Product 2", "Variant 2", 20000m, 1);

        // Act
        targetCart.MergeFrom(sourceCart);

        // Assert
        targetCart.Items.Count().ShouldBe(2);
        targetCart.ItemCount.ShouldBe(3); // 2 + 1
    }

    [Fact]
    public void MergeFrom_WithOverlappingProductVariant_ShouldMergeQuantities()
    {
        // Arrange
        var targetCart = CreateActiveUserCart();
        targetCart.AddItem(ProductId1, VariantId1, "Product 1", "Variant 1", 10000m, 2);

        var sourceCart = CreateActiveGuestCart();
        sourceCart.AddItem(ProductId1, VariantId1, "Product 1", "Variant 1", 10000m, 3);

        // Act
        targetCart.MergeFrom(sourceCart);

        // Assert
        targetCart.Items.Count().ShouldBe(1);
        targetCart.Items.First().Quantity.ShouldBe(5); // 2 + 3
    }

    [Fact]
    public void MergeFrom_IntoInactiveCart_ShouldThrow()
    {
        // Arrange
        var targetCart = CreateActiveUserCart();
        targetCart.MarkAsAbandoned();
        var sourceCart = CreateActiveGuestCart();
        sourceCart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m);

        // Act
        var act = () => targetCart.MergeFrom(sourceCart);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Cannot merge into an inactive cart");
    }

    [Fact]
    public void MergeFrom_EmptySourceCart_ShouldNotChangeTargetCart()
    {
        // Arrange
        var targetCart = CreateActiveUserCart();
        targetCart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m, 1);
        var sourceCart = CreateActiveGuestCart();

        // Act
        targetCart.MergeFrom(sourceCart);

        // Assert
        targetCart.Items.Count().ShouldBe(1);
    }

    #endregion

    #region Status Transition: MarkAsAbandoned Tests

    [Fact]
    public void MarkAsAbandoned_ActiveCart_ShouldTransitionToAbandoned()
    {
        // Arrange
        var cart = CreateCartWithItems();

        // Act
        cart.MarkAsAbandoned();

        // Assert
        cart.Status.ShouldBe(CartStatus.Abandoned);
    }

    [Fact]
    public void MarkAsAbandoned_ActiveCart_ShouldRaiseCartAbandonedEvent()
    {
        // Arrange
        var cart = CreateCartWithItems();
        cart.ClearDomainEvents();

        // Act
        cart.MarkAsAbandoned();

        // Assert
        var abandonedEvent = cart.DomainEvents.OfType<CartAbandonedEvent>().Single();
        abandonedEvent.CartId.ShouldBe(cart.Id);
        abandonedEvent.UserId.ShouldBe(TestUserId);
        abandonedEvent.ItemCount.ShouldBe(cart.ItemCount);
        abandonedEvent.Subtotal.ShouldBe(cart.Subtotal);
    }

    [Fact]
    public void MarkAsAbandoned_AlreadyConvertedCart_ShouldNotChangeStatus()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsConverted(Guid.NewGuid());

        // Act
        cart.MarkAsAbandoned();

        // Assert
        cart.Status.ShouldBe(CartStatus.Converted);
    }

    [Fact]
    public void MarkAsAbandoned_AlreadyAbandonedCart_ShouldNotRaiseEvent()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsAbandoned();
        cart.ClearDomainEvents();

        // Act
        cart.MarkAsAbandoned();

        // Assert
        cart.DomainEvents.ShouldBeEmpty();
    }

    #endregion

    #region Status Transition: MarkAsConverted Tests

    [Fact]
    public void MarkAsConverted_ActiveCart_ShouldTransitionToConverted()
    {
        // Arrange
        var cart = CreateCartWithItems();
        var orderId = Guid.NewGuid();

        // Act
        cart.MarkAsConverted(orderId);

        // Assert
        cart.Status.ShouldBe(CartStatus.Converted);
    }

    [Fact]
    public void MarkAsConverted_AbandonedCart_ShouldTransitionToConverted()
    {
        // Arrange
        var cart = CreateCartWithItems();
        cart.MarkAsAbandoned();

        // Act
        cart.MarkAsConverted(Guid.NewGuid());

        // Assert
        cart.Status.ShouldBe(CartStatus.Converted);
    }

    [Fact]
    public void MarkAsConverted_ShouldRaiseCartConvertedEvent()
    {
        // Arrange
        var cart = CreateCartWithItems();
        cart.ClearDomainEvents();
        var orderId = Guid.NewGuid();

        // Act
        cart.MarkAsConverted(orderId);

        // Assert
        var convertedEvent = cart.DomainEvents.OfType<CartConvertedEvent>().Single();
        convertedEvent.CartId.ShouldBe(cart.Id);
        convertedEvent.OrderId.ShouldBe(orderId);
        convertedEvent.UserId.ShouldBe(TestUserId);
    }

    [Fact]
    public void MarkAsConverted_ExpiredCart_ShouldNotChangeStatus()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsExpired();

        // Act
        cart.MarkAsConverted(Guid.NewGuid());

        // Assert
        cart.Status.ShouldBe(CartStatus.Expired);
    }

    #endregion

    #region Status Transition: MarkAsExpired Tests

    [Fact]
    public void MarkAsExpired_ActiveCart_ShouldTransitionToExpired()
    {
        // Arrange
        var cart = CreateActiveGuestCart();

        // Act
        cart.MarkAsExpired();

        // Assert
        cart.Status.ShouldBe(CartStatus.Expired);
    }

    [Fact]
    public void MarkAsExpired_AbandonedCart_ShouldTransitionToExpired()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsAbandoned();

        // Act
        cart.MarkAsExpired();

        // Assert
        cart.Status.ShouldBe(CartStatus.Expired);
    }

    [Fact]
    public void MarkAsExpired_ConvertedCart_ShouldNotChangeStatus()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsConverted(Guid.NewGuid());

        // Act
        cart.MarkAsExpired();

        // Assert
        cart.Status.ShouldBe(CartStatus.Converted);
    }

    #endregion

    #region Status Transition: MarkAsMerged Tests

    [Fact]
    public void MarkAsMerged_ActiveCart_ShouldTransitionToMerged()
    {
        // Arrange
        var cart = CreateActiveGuestCart();
        var targetCartId = Guid.NewGuid();

        // Act
        cart.MarkAsMerged(targetCartId, TestUserId, 3);

        // Assert
        cart.Status.ShouldBe(CartStatus.Merged);
    }

    [Fact]
    public void MarkAsMerged_ShouldRaiseCartMergedEvent()
    {
        // Arrange
        var cart = CreateActiveGuestCart();
        cart.ClearDomainEvents();
        var targetCartId = Guid.NewGuid();

        // Act
        cart.MarkAsMerged(targetCartId, TestUserId, 5);

        // Assert
        var mergedEvent = cart.DomainEvents.OfType<CartMergedEvent>().Single();
        mergedEvent.SourceCartId.ShouldBe(cart.Id);
        mergedEvent.TargetCartId.ShouldBe(targetCartId);
        mergedEvent.UserId.ShouldBe(TestUserId);
        mergedEvent.MergedItemCount.ShouldBe(5);
    }

    [Fact]
    public void MarkAsMerged_NonActiveCart_ShouldNotChangeStatus()
    {
        // Arrange
        var cart = CreateActiveGuestCart();
        cart.MarkAsAbandoned();

        // Act
        cart.MarkAsMerged(Guid.NewGuid(), TestUserId, 0);

        // Assert
        cart.Status.ShouldBe(CartStatus.Abandoned);
    }

    #endregion

    #region Status Transition: Reactivate Tests

    [Fact]
    public void Reactivate_AbandonedCart_ShouldTransitionToActive()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsAbandoned();

        // Act
        cart.Reactivate();

        // Assert
        cart.Status.ShouldBe(CartStatus.Active);
    }

    [Fact]
    public void Reactivate_AbandonedCart_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsAbandoned();
        var beforeReactivate = DateTimeOffset.UtcNow;

        // Act
        cart.Reactivate();

        // Assert
        cart.LastActivityAt.ShouldBeGreaterThanOrEqualTo(beforeReactivate);
    }

    [Fact]
    public void Reactivate_ActiveCart_ShouldRemainActive()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act
        cart.Reactivate();

        // Assert
        cart.Status.ShouldBe(CartStatus.Active);
    }

    [Fact]
    public void Reactivate_ConvertedCart_ShouldNotChangeStatus()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsConverted(Guid.NewGuid());

        // Act
        cart.Reactivate();

        // Assert
        cart.Status.ShouldBe(CartStatus.Converted);
    }

    [Fact]
    public void Reactivate_ExpiredCart_ShouldNotChangeStatus()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.MarkAsExpired();

        // Act
        cart.Reactivate();

        // Assert
        cart.Status.ShouldBe(CartStatus.Expired);
    }

    #endregion

    #region AssociateWithUser Tests

    [Fact]
    public void AssociateWithUser_GuestCart_ShouldSetUserIdAndClearSession()
    {
        // Arrange
        var cart = CreateActiveGuestCart();

        // Act
        cart.AssociateWithUser(TestUserId);

        // Assert
        cart.UserId.ShouldBe(TestUserId);
        cart.SessionId.ShouldBeNull();
        cart.ExpiresAt.ShouldBeNull();
        cart.IsGuest.ShouldBeFalse();
    }

    [Fact]
    public void AssociateWithUser_ShouldUpdateLastActivityAt()
    {
        // Arrange
        var cart = CreateActiveGuestCart();
        var beforeAssociate = DateTimeOffset.UtcNow;

        // Act
        cart.AssociateWithUser(TestUserId);

        // Assert
        cart.LastActivityAt.ShouldBeGreaterThanOrEqualTo(beforeAssociate);
    }

    [Fact]
    public void AssociateWithUser_WithEmptyUserId_ShouldThrow()
    {
        // Arrange
        var cart = CreateActiveGuestCart();

        // Act
        var act = () => cart.AssociateWithUser(string.Empty);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("UserId cannot be empty");
    }

    [Fact]
    public void AssociateWithUser_WithNullUserId_ShouldThrow()
    {
        // Arrange
        var cart = CreateActiveGuestCart();

        // Act
        var act = () => cart.AssociateWithUser(null!);

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("UserId cannot be empty");
    }

    #endregion

    #region IsOwnedBy Tests

    [Fact]
    public void IsOwnedBy_MatchingUserId_ShouldReturnTrue()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act & Assert
        cart.IsOwnedBy(TestUserId, null).ShouldBeTrue();
    }

    [Fact]
    public void IsOwnedBy_MatchingSessionId_ShouldReturnTrue()
    {
        // Arrange
        var cart = CreateActiveGuestCart();

        // Act & Assert
        cart.IsOwnedBy(null, TestSessionId).ShouldBeTrue();
    }

    [Fact]
    public void IsOwnedBy_NonMatchingIds_ShouldReturnFalse()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act & Assert
        cart.IsOwnedBy("other-user", "other-session").ShouldBeFalse();
    }

    [Fact]
    public void IsOwnedBy_BothNull_ShouldReturnFalse()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act & Assert
        cart.IsOwnedBy(null, null).ShouldBeFalse();
    }

    [Fact]
    public void IsOwnedBy_BothEmpty_ShouldReturnFalse()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act & Assert
        cart.IsOwnedBy(string.Empty, string.Empty).ShouldBeFalse();
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void ItemCount_ShouldSumAllQuantities()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.AddItem(ProductId1, VariantId1, "Product 1", "Variant 1", 10000m, 3);
        cart.AddItem(ProductId2, VariantId2, "Product 2", "Variant 2", 20000m, 5);

        // Act & Assert
        cart.ItemCount.ShouldBe(8); // 3 + 5
    }

    [Fact]
    public void Subtotal_ShouldSumAllLineTotals()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.AddItem(ProductId1, VariantId1, "Product 1", "Variant 1", 10000m, 3); // 30,000
        cart.AddItem(ProductId2, VariantId2, "Product 2", "Variant 2", 20000m, 2); // 40,000

        // Act & Assert
        cart.Subtotal.ShouldBe(70000m);
    }

    [Fact]
    public void IsEmpty_WithNoItems_ShouldReturnTrue()
    {
        // Arrange
        var cart = CreateActiveUserCart();

        // Act & Assert
        cart.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void IsEmpty_WithItems_ShouldReturnFalse()
    {
        // Arrange
        var cart = CreateActiveUserCart();
        cart.AddItem(ProductId1, VariantId1, "Product", "Variant", 10000m);

        // Act & Assert
        cart.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void IsGuest_UserCart_ShouldReturnFalse()
    {
        // Act
        var cart = CreateActiveUserCart();

        // Assert
        cart.IsGuest.ShouldBeFalse();
    }

    [Fact]
    public void IsGuest_GuestCart_ShouldReturnTrue()
    {
        // Act
        var cart = CreateActiveGuestCart();

        // Assert
        cart.IsGuest.ShouldBeTrue();
    }

    #endregion
}
