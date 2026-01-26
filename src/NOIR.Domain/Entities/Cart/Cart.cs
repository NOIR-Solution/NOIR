namespace NOIR.Domain.Entities.Cart;

/// <summary>
/// Shopping cart for e-commerce.
/// Supports both guest (session-based) and authenticated users.
/// </summary>
public class Cart : TenantAggregateRoot<Guid>
{
    // User association
    /// <summary>
    /// User ID if authenticated, null for guest carts.
    /// </summary>
    public string? UserId { get; private set; }

    /// <summary>
    /// Session ID for guest cart identification.
    /// </summary>
    public string? SessionId { get; private set; }

    // Status
    public CartStatus Status { get; private set; }

    /// <summary>
    /// Last activity timestamp for abandonment tracking.
    /// </summary>
    public DateTimeOffset LastActivityAt { get; private set; }

    /// <summary>
    /// Timestamp when cart expires (for guest carts).
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    // Currency
    public string Currency { get; private set; } = "VND";

    // Navigation
    public virtual ICollection<CartItem> Items { get; private set; } = new List<CartItem>();

    // Computed
    public int ItemCount => Items.Sum(i => i.Quantity);
    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public bool IsEmpty => !Items.Any();
    public bool IsGuest => string.IsNullOrEmpty(UserId);

    // Private constructor for EF Core
    private Cart() : base() { }

    private Cart(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a cart for an authenticated user.
    /// </summary>
    public static Cart CreateForUser(string userId, string currency = "VND", string? tenantId = null)
    {
        var cart = new Cart(Guid.NewGuid(), tenantId)
        {
            UserId = userId,
            SessionId = null,
            Status = CartStatus.Active,
            Currency = currency,
            LastActivityAt = DateTimeOffset.UtcNow,
            ExpiresAt = null
        };

        cart.AddDomainEvent(new CartCreatedEvent(cart.Id, userId, null));
        return cart;
    }

    /// <summary>
    /// Factory method to create a cart for a guest session.
    /// </summary>
    public static Cart CreateForGuest(string sessionId, string currency = "VND", string? tenantId = null)
    {
        var cart = new Cart(Guid.NewGuid(), tenantId)
        {
            UserId = null,
            SessionId = sessionId,
            Status = CartStatus.Active,
            Currency = currency,
            LastActivityAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        cart.AddDomainEvent(new CartCreatedEvent(cart.Id, null, sessionId));
        return cart;
    }

    /// <summary>
    /// Adds an item to the cart or updates quantity if same product+variant exists.
    /// </summary>
    public CartItem AddItem(
        Guid productId,
        Guid productVariantId,
        string productName,
        string variantName,
        decimal unitPrice,
        int quantity = 1,
        string? imageUrl = null)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero");

        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot add items to an inactive cart");

        // Check for existing item with same product+variant
        var existingItem = Items.FirstOrDefault(i =>
            i.ProductId == productId && i.ProductVariantId == productVariantId);

        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
            UpdateActivity();
            return existingItem;
        }

        var item = CartItem.Create(
            Id,
            productId,
            productVariantId,
            productName,
            variantName,
            unitPrice,
            quantity,
            imageUrl,
            TenantId);

        Items.Add(item);
        UpdateActivity();

        AddDomainEvent(new CartItemAddedEvent(Id, item.Id, productId, productVariantId, quantity));
        return item;
    }

    /// <summary>
    /// Updates quantity for a specific cart item.
    /// </summary>
    public void UpdateItemQuantity(Guid itemId, int quantity)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot update items in an inactive cart");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null)
            throw new InvalidOperationException("Cart item not found");

        if (quantity <= 0)
        {
            RemoveItem(itemId);
            return;
        }

        var oldQuantity = item.Quantity;
        item.UpdateQuantity(quantity);
        UpdateActivity();

        AddDomainEvent(new CartItemQuantityUpdatedEvent(Id, itemId, oldQuantity, quantity));
    }

    /// <summary>
    /// Checks if the cart is owned by the specified user or session.
    /// </summary>
    public bool IsOwnedBy(string? userId, string? sessionId)
    {
        return (!string.IsNullOrEmpty(userId) && UserId == userId) ||
               (!string.IsNullOrEmpty(sessionId) && SessionId == sessionId);
    }

    /// <summary>
    /// Removes an item from the cart.
    /// </summary>
    public void RemoveItem(Guid itemId)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot remove items from an inactive cart");

        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            Items.Remove(item);
            UpdateActivity();
            AddDomainEvent(new CartItemRemovedEvent(Id, itemId, item.ProductId, item.ProductVariantId));
        }
    }

    /// <summary>
    /// Clears all items from the cart.
    /// </summary>
    public void Clear()
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot clear an inactive cart");

        Items.Clear();
        UpdateActivity();
    }

    /// <summary>
    /// Merges items from another cart (typically guest cart into user cart).
    /// </summary>
    public void MergeFrom(Cart sourceCart)
    {
        if (Status != CartStatus.Active)
            throw new InvalidOperationException("Cannot merge into an inactive cart");

        foreach (var item in sourceCart.Items)
        {
            AddItem(
                item.ProductId,
                item.ProductVariantId,
                item.ProductName,
                item.VariantName,
                item.UnitPrice,
                item.Quantity,
                item.ImageUrl);
        }

        UpdateActivity();
    }

    /// <summary>
    /// Marks the cart as abandoned after inactivity period.
    /// </summary>
    public void MarkAsAbandoned()
    {
        if (Status == CartStatus.Active)
        {
            Status = CartStatus.Abandoned;
            AddDomainEvent(new CartAbandonedEvent(Id, UserId, SessionId, ItemCount, Subtotal));
        }
    }

    /// <summary>
    /// Marks the cart as converted when checkout completes.
    /// </summary>
    public void MarkAsConverted(Guid orderId)
    {
        if (Status == CartStatus.Active || Status == CartStatus.Abandoned)
        {
            Status = CartStatus.Converted;
            AddDomainEvent(new CartConvertedEvent(Id, orderId, UserId, ItemCount, Subtotal));
        }
    }

    /// <summary>
    /// Marks the cart as expired (for guest carts).
    /// </summary>
    public void MarkAsExpired()
    {
        if (Status == CartStatus.Active || Status == CartStatus.Abandoned)
        {
            Status = CartStatus.Expired;
        }
    }

    /// <summary>
    /// Marks the cart as merged (source cart after merge operation).
    /// </summary>
    public void MarkAsMerged(Guid targetCartId, string userId, int mergedItemCount)
    {
        if (Status == CartStatus.Active)
        {
            Status = CartStatus.Merged;
            AddDomainEvent(new CartMergedEvent(Id, targetCartId, userId, mergedItemCount));
        }
    }

    /// <summary>
    /// Associates the cart with an authenticated user (after login).
    /// </summary>
    public void AssociateWithUser(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            throw new InvalidOperationException("UserId cannot be empty");

        UserId = userId;
        SessionId = null;
        ExpiresAt = null;
        UpdateActivity();
    }

    /// <summary>
    /// Reactivates an abandoned cart.
    /// </summary>
    public void Reactivate()
    {
        if (Status == CartStatus.Abandoned)
        {
            Status = CartStatus.Active;
            UpdateActivity();
        }
    }

    private void UpdateActivity()
    {
        LastActivityAt = DateTimeOffset.UtcNow;
    }
}
