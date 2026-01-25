namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product variant (size, color, etc.).
/// </summary>
public class ProductVariant : TenantEntity<Guid>
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Sku { get; private set; }
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }

    /// <summary>
    /// Stock quantity with concurrency check for safe updates.
    /// </summary>
    [ConcurrencyCheck]
    public int StockQuantity { get; private set; }

    /// <summary>
    /// Flexible attributes as JSON (e.g., {"color": "Red", "size": "M"}).
    /// </summary>
    public string? OptionsJson { get; private set; }

    public int SortOrder { get; private set; }

    // Navigation
    public virtual Product Product { get; private set; } = null!;

    // Computed
    public bool InStock => StockQuantity > 0;
    public bool LowStock => StockQuantity > 0 && StockQuantity <= 5;
    public bool OnSale => CompareAtPrice.HasValue && CompareAtPrice > Price;

    // Private constructor for EF Core
    private ProductVariant() { }

    /// <summary>
    /// Factory method to create a new variant.
    /// </summary>
    internal static ProductVariant Create(
        Guid productId,
        string name,
        decimal price,
        string? sku,
        Dictionary<string, string>? options,
        string? tenantId)
    {
        return new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            Name = name,
            Price = price,
            Sku = sku,
            StockQuantity = 0,
            OptionsJson = options != null ? JsonSerializer.Serialize(options) : null,
            SortOrder = 0
        };
    }

    /// <summary>
    /// Updates variant details.
    /// </summary>
    public void UpdateDetails(string name, decimal price, string? sku)
    {
        Name = name;
        Price = price;
        Sku = sku;
    }

    /// <summary>
    /// Sets the compare-at (original) price for sales display.
    /// </summary>
    public void SetCompareAtPrice(decimal? compareAtPrice)
    {
        CompareAtPrice = compareAtPrice;
    }

    /// <summary>
    /// Updates the variant options.
    /// </summary>
    public void UpdateOptions(Dictionary<string, string> options)
    {
        OptionsJson = JsonSerializer.Serialize(options);
    }

    /// <summary>
    /// Gets the variant options as a dictionary.
    /// </summary>
    public Dictionary<string, string> GetOptions()
    {
        if (string.IsNullOrEmpty(OptionsJson))
            return new Dictionary<string, string>();

        return JsonSerializer.Deserialize<Dictionary<string, string>>(OptionsJson)
            ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Reserve stock for checkout. Throws if insufficient.
    /// </summary>
    public void ReserveStock(int quantity)
    {
        if (StockQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {StockQuantity}, Requested: {quantity}");

        StockQuantity -= quantity;
    }

    /// <summary>
    /// Release reserved stock back to inventory.
    /// </summary>
    public void ReleaseStock(int quantity)
    {
        StockQuantity += quantity;
    }

    /// <summary>
    /// Adjust stock by delta (positive or negative).
    /// </summary>
    public void AdjustStock(int delta)
    {
        var newQuantity = StockQuantity + delta;
        if (newQuantity < 0)
            throw new InvalidOperationException("Stock cannot be negative");

        StockQuantity = newQuantity;
    }

    /// <summary>
    /// Set absolute stock quantity.
    /// </summary>
    public void SetStock(int quantity)
    {
        if (quantity < 0)
            throw new InvalidOperationException("Stock cannot be negative");

        StockQuantity = quantity;
    }

    /// <summary>
    /// Sets the sort order.
    /// </summary>
    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }
}
