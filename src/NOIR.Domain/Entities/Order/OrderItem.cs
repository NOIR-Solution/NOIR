namespace NOIR.Domain.Entities.Order;

/// <summary>
/// Represents an item in an order.
/// Captures product details at time of purchase (denormalized).
/// </summary>
public class OrderItem : TenantEntity<Guid>
{
    private OrderItem() : base() { }
    private OrderItem(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Parent order ID.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// Reference to original product.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Reference to original product variant.
    /// </summary>
    public Guid ProductVariantId { get; private set; }

    /// <summary>
    /// Product name at time of purchase.
    /// </summary>
    public string ProductName { get; private set; } = string.Empty;

    /// <summary>
    /// Variant name at time of purchase.
    /// </summary>
    public string VariantName { get; private set; } = string.Empty;

    /// <summary>
    /// SKU at time of purchase.
    /// </summary>
    public string? Sku { get; private set; }

    /// <summary>
    /// Product image URL at time of purchase.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Variant options at time of purchase (e.g., "Color: Red, Size: M").
    /// </summary>
    public string? OptionsSnapshot { get; private set; }

    /// <summary>
    /// Unit price at time of purchase.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Quantity ordered.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Discount applied to this item.
    /// </summary>
    public decimal DiscountAmount { get; private set; }

    /// <summary>
    /// Tax amount for this item.
    /// </summary>
    public decimal TaxAmount { get; private set; }

    /// <summary>
    /// Line total: (UnitPrice * Quantity) - DiscountAmount + TaxAmount.
    /// </summary>
    public decimal LineTotal => (UnitPrice * Quantity) - DiscountAmount + TaxAmount;

    /// <summary>
    /// Subtotal before discount and tax: UnitPrice * Quantity.
    /// </summary>
    public decimal Subtotal => UnitPrice * Quantity;

    /// <summary>
    /// Navigation to parent order.
    /// </summary>
    public virtual Order? Order { get; private set; }

    /// <summary>
    /// Creates a new order item.
    /// </summary>
    public static OrderItem Create(
        Guid orderId,
        Guid productId,
        Guid productVariantId,
        string productName,
        string variantName,
        decimal unitPrice,
        int quantity,
        string? sku = null,
        string? imageUrl = null,
        string? optionsSnapshot = null,
        string? tenantId = null)
    {
        return new OrderItem(Guid.NewGuid(), tenantId)
        {
            OrderId = orderId,
            ProductId = productId,
            ProductVariantId = productVariantId,
            ProductName = productName,
            VariantName = variantName,
            UnitPrice = unitPrice,
            Quantity = quantity,
            Sku = sku,
            ImageUrl = imageUrl,
            OptionsSnapshot = optionsSnapshot,
            DiscountAmount = 0,
            TaxAmount = 0
        };
    }

    /// <summary>
    /// Sets the discount amount for this item.
    /// </summary>
    public void SetDiscount(decimal discountAmount)
    {
        if (discountAmount < 0)
            throw new InvalidOperationException("Discount amount cannot be negative");

        DiscountAmount = discountAmount;
    }

    /// <summary>
    /// Sets the tax amount for this item.
    /// </summary>
    public void SetTax(decimal taxAmount)
    {
        if (taxAmount < 0)
            throw new InvalidOperationException("Tax amount cannot be negative");

        TaxAmount = taxAmount;
    }
}
