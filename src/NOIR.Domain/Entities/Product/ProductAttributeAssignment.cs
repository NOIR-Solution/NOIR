namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Stores a product's actual attribute value assignments.
/// This is a junction table between Product and ProductAttribute with polymorphic value storage.
/// </summary>
public class ProductAttributeAssignment : TenantEntity<Guid>
{
    // Foreign Keys
    public Guid ProductId { get; private set; }
    public Guid AttributeId { get; private set; }
    public Guid? VariantId { get; private set; }  // If variant-specific

    // Value storage (only one used based on Attribute.Type)
    public Guid? AttributeValueId { get; private set; }           // For Select type
    public string? AttributeValueIds { get; private set; }        // For MultiSelect type (JSON array of Guids)
    public string? TextValue { get; private set; }                // For Text, TextArea, Url types
    public decimal? NumberValue { get; private set; }             // For Number, Decimal types
    public bool? BoolValue { get; private set; }                  // For Boolean type
    public DateTime? DateValue { get; private set; }              // For Date type
    public DateTime? DateTimeValue { get; private set; }          // For DateTime type
    public string? ColorValue { get; private set; }               // For Color type (#RRGGBB)
    public decimal? MinRangeValue { get; private set; }           // For Range type min
    public decimal? MaxRangeValue { get; private set; }           // For Range type max
    public string? FileUrl { get; private set; }                  // For File type

    // Computed display value (for search/filtering)
    public string? DisplayValue { get; private set; }

    // Navigation
    public virtual Product Product { get; private set; } = null!;
    public virtual ProductAttribute Attribute { get; private set; } = null!;
    public virtual ProductAttributeValue? SelectedValue { get; private set; }
    public virtual ProductVariant? Variant { get; private set; }

    // Private constructor for EF Core
    private ProductAttributeAssignment() : base() { }

    private ProductAttributeAssignment(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new attribute assignment.
    /// </summary>
    public static ProductAttributeAssignment Create(
        Guid productId,
        Guid attributeId,
        Guid? variantId = null,
        string? tenantId = null)
    {
        return new ProductAttributeAssignment(Guid.NewGuid(), tenantId)
        {
            ProductId = productId,
            AttributeId = attributeId,
            VariantId = variantId
        };
    }

    /// <summary>
    /// Sets a Select type value (single predefined value).
    /// </summary>
    public void SetSelectValue(Guid attributeValueId, string displayValue)
    {
        ClearAllValues();
        AttributeValueId = attributeValueId;
        DisplayValue = displayValue;
    }

    /// <summary>
    /// Sets a MultiSelect type value (multiple predefined values).
    /// </summary>
    public void SetMultiSelectValue(List<Guid> attributeValueIds, string displayValue)
    {
        ClearAllValues();
        AttributeValueIds = System.Text.Json.JsonSerializer.Serialize(attributeValueIds);
        DisplayValue = displayValue;
    }

    /// <summary>
    /// Sets a Text, TextArea, or Url type value.
    /// </summary>
    public void SetTextValue(string value)
    {
        ClearAllValues();
        TextValue = value;
        DisplayValue = value;
    }

    /// <summary>
    /// Sets a Number or Decimal type value.
    /// </summary>
    public void SetNumberValue(decimal value, string? unit = null)
    {
        ClearAllValues();
        NumberValue = value;
        DisplayValue = unit != null ? $"{value} {unit}" : value.ToString();
    }

    /// <summary>
    /// Sets a Boolean type value.
    /// </summary>
    public void SetBoolValue(bool value)
    {
        ClearAllValues();
        BoolValue = value;
        DisplayValue = value ? "Yes" : "No";
    }

    /// <summary>
    /// Sets a Date type value.
    /// </summary>
    public void SetDateValue(DateTime value)
    {
        ClearAllValues();
        DateValue = value.Date;
        DisplayValue = value.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Sets a DateTime type value.
    /// </summary>
    public void SetDateTimeValue(DateTime value)
    {
        ClearAllValues();
        DateTimeValue = value;
        DisplayValue = value.ToString("yyyy-MM-dd HH:mm:ss");
    }

    /// <summary>
    /// Sets a Color type value.
    /// </summary>
    public void SetColorValue(string hexColor)
    {
        ClearAllValues();
        ColorValue = hexColor;
        DisplayValue = hexColor;
    }

    /// <summary>
    /// Sets a Range type value.
    /// </summary>
    public void SetRangeValue(decimal min, decimal max, string? unit = null)
    {
        ClearAllValues();
        MinRangeValue = min;
        MaxRangeValue = max;
        DisplayValue = unit != null ? $"{min} - {max} {unit}" : $"{min} - {max}";
    }

    /// <summary>
    /// Sets a File type value.
    /// </summary>
    public void SetFileValue(string fileUrl)
    {
        ClearAllValues();
        FileUrl = fileUrl;
        DisplayValue = fileUrl;
    }

    /// <summary>
    /// Clears all value columns before setting a new value.
    /// </summary>
    private void ClearAllValues()
    {
        AttributeValueId = null;
        AttributeValueIds = null;
        TextValue = null;
        NumberValue = null;
        BoolValue = null;
        DateValue = null;
        DateTimeValue = null;
        ColorValue = null;
        MinRangeValue = null;
        MaxRangeValue = null;
        FileUrl = null;
        DisplayValue = null;
    }

    /// <summary>
    /// Gets the typed value based on the attribute type.
    /// </summary>
    public object? GetTypedValue()
    {
        if (AttributeValueId.HasValue) return AttributeValueId.Value;
        if (!string.IsNullOrEmpty(AttributeValueIds))
            return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(AttributeValueIds);
        if (!string.IsNullOrEmpty(TextValue)) return TextValue;
        if (NumberValue.HasValue) return NumberValue.Value;
        if (BoolValue.HasValue) return BoolValue.Value;
        if (DateValue.HasValue) return DateValue.Value;
        if (DateTimeValue.HasValue) return DateTimeValue.Value;
        if (!string.IsNullOrEmpty(ColorValue)) return ColorValue;
        if (MinRangeValue.HasValue && MaxRangeValue.HasValue)
            return new { Min = MinRangeValue.Value, Max = MaxRangeValue.Value };
        if (!string.IsNullOrEmpty(FileUrl)) return FileUrl;

        return null;
    }

    /// <summary>
    /// Checks if this assignment has a value set.
    /// </summary>
    public bool HasValue => GetTypedValue() != null;

    /// <summary>
    /// Gets the MultiSelect value IDs as a list.
    /// </summary>
    public List<Guid> GetMultiSelectValueIds()
    {
        if (string.IsNullOrEmpty(AttributeValueIds))
            return new List<Guid>();

        return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(AttributeValueIds)
               ?? new List<Guid>();
    }
}
