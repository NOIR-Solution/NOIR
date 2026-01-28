namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product option type (e.g., "Color", "Size", "Material").
/// Options can be shared across products or product-specific.
/// </summary>
public class ProductOption : TenantEntity<Guid>
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? DisplayName { get; private set; }
    public int SortOrder { get; private set; }

    // Navigation
    public virtual Product Product { get; private set; } = null!;
    public virtual ICollection<ProductOptionValue> Values { get; private set; } = new List<ProductOptionValue>();

    // Private constructor for EF Core
    private ProductOption() { }

    /// <summary>
    /// Factory method to create a new product option.
    /// </summary>
    internal static ProductOption Create(
        Guid productId,
        string name,
        string? displayName,
        int sortOrder,
        string? tenantId)
    {
        return new ProductOption
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            Name = name.ToLowerInvariant().Replace(" ", "_"),
            DisplayName = displayName ?? name,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Updates the option details.
    /// </summary>
    public void Update(string name, string? displayName, int sortOrder)
    {
        Name = name.ToLowerInvariant().Replace(" ", "_");
        DisplayName = displayName ?? name;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Adds a new value to this option.
    /// </summary>
    public ProductOptionValue AddValue(string value, string? displayValue = null)
    {
        var sortOrder = Values.Any() ? Values.Max(v => v.SortOrder) + 1 : 0;
        var optionValue = ProductOptionValue.Create(Id, value, displayValue, sortOrder, TenantId);
        Values.Add(optionValue);
        return optionValue;
    }

    /// <summary>
    /// Removes a value from this option.
    /// </summary>
    public void RemoveValue(Guid valueId)
    {
        var value = Values.FirstOrDefault(v => v.Id == valueId);
        if (value != null)
        {
            Values.Remove(value);
        }
    }
}
