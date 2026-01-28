namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Specific value for a product option (e.g., "Red" for Color option).
/// </summary>
public class ProductOptionValue : TenantEntity<Guid>
{
    public Guid OptionId { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public string? DisplayValue { get; private set; }

    /// <summary>
    /// Optional color code for visual display (e.g., "#FF0000" for Red).
    /// </summary>
    public string? ColorCode { get; private set; }

    /// <summary>
    /// Optional image URL for swatch display.
    /// </summary>
    public string? SwatchUrl { get; private set; }

    public int SortOrder { get; private set; }

    // Navigation
    public virtual ProductOption Option { get; private set; } = null!;

    // Private constructor for EF Core
    private ProductOptionValue() { }

    /// <summary>
    /// Factory method to create a new option value.
    /// </summary>
    internal static ProductOptionValue Create(
        Guid optionId,
        string value,
        string? displayValue,
        int sortOrder,
        string? tenantId)
    {
        return new ProductOptionValue
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            OptionId = optionId,
            Value = value.ToLowerInvariant().Replace(" ", "_"),
            DisplayValue = displayValue ?? value,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Updates the value details.
    /// </summary>
    public void Update(string value, string? displayValue, int sortOrder)
    {
        Value = value.ToLowerInvariant().Replace(" ", "_");
        DisplayValue = displayValue ?? value;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Sets the color code for visual display.
    /// </summary>
    public void SetColorCode(string? colorCode)
    {
        ColorCode = colorCode;
    }

    /// <summary>
    /// Sets the swatch image URL.
    /// </summary>
    public void SetSwatchUrl(string? swatchUrl)
    {
        SwatchUrl = swatchUrl;
    }
}
