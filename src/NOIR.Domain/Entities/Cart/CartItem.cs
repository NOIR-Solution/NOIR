namespace NOIR.Domain.Entities.Cart;

/// <summary>
/// Individual item in a shopping cart.
/// Stores a snapshot of product information at time of adding to cart.
/// </summary>
public class CartItem : TenantEntity<Guid>
{
    // Cart association
    public Guid CartId { get; private set; }

    // Product reference
    public Guid ProductId { get; private set; }
    public Guid ProductVariantId { get; private set; }

    // Snapshot of product info (denormalized for performance and historical accuracy)
    public string ProductName { get; private set; } = string.Empty;
    public string VariantName { get; private set; } = string.Empty;
    public string? ImageUrl { get; private set; }

    // Pricing
    public decimal UnitPrice { get; private set; }

    // Quantity
    public int Quantity { get; private set; }

    // Navigation
    public virtual Cart Cart { get; private set; } = null!;

    // Computed
    public decimal LineTotal => UnitPrice * Quantity;

    // Private constructor for EF Core
    private CartItem() { }

    /// <summary>
    /// Factory method to create a new cart item.
    /// </summary>
    internal static CartItem Create(
        Guid cartId,
        Guid productId,
        Guid productVariantId,
        string productName,
        string variantName,
        decimal unitPrice,
        int quantity,
        string? imageUrl,
        string? tenantId)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero");

        if (unitPrice < 0)
            throw new InvalidOperationException("Unit price cannot be negative");

        return new CartItem
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CartId = cartId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ProductName = productName,
            VariantName = variantName,
            UnitPrice = unitPrice,
            Quantity = quantity,
            ImageUrl = imageUrl
        };
    }

    /// <summary>
    /// Updates the quantity of this cart item.
    /// </summary>
    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero");

        Quantity = quantity;
    }

    /// <summary>
    /// Updates the unit price (e.g., when price changes during session).
    /// </summary>
    public void UpdatePrice(decimal unitPrice)
    {
        if (unitPrice < 0)
            throw new InvalidOperationException("Unit price cannot be negative");

        UnitPrice = unitPrice;
    }

    /// <summary>
    /// Updates the product snapshot (e.g., if product details changed).
    /// </summary>
    public void UpdateProductSnapshot(
        string productName,
        string variantName,
        string? imageUrl,
        decimal unitPrice)
    {
        ProductName = productName;
        VariantName = variantName;
        ImageUrl = imageUrl;
        UnitPrice = unitPrice;
    }
}
