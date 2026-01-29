namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product attribute definition for flexible product specifications.
/// Supports 13 data types with type-specific configuration.
/// </summary>
public class ProductAttribute : TenantAggregateRoot<Guid>
{
    // Identity
    public string Code { get; private set; } = string.Empty;      // "screen_size"
    public string Name { get; private set; } = string.Empty;      // "Screen Size"
    public AttributeType Type { get; private set; }

    // Behavior flags
    public bool IsFilterable { get; private set; }                // Show in filter sidebar
    public bool IsSearchable { get; private set; }                // Include in search index
    public bool IsRequired { get; private set; }                  // Mandatory for products
    public bool IsVariantAttribute { get; private set; }          // Creates variants
    public bool ShowInProductCard { get; private set; }           // Show in list view
    public bool ShowInSpecifications { get; private set; } = true; // Show in spec table

    // Type-specific configuration
    public string? Unit { get; private set; }                     // "inch", "mAh", "kg"
    public string? ValidationRegex { get; private set; }          // For Text type
    public decimal? MinValue { get; private set; }                // For Number/Decimal
    public decimal? MaxValue { get; private set; }                // For Number/Decimal
    public int? MaxLength { get; private set; }                   // For Text/TextArea
    public string? DefaultValue { get; private set; }             // Default when creating
    public string? Placeholder { get; private set; }              // Input placeholder
    public string? HelpText { get; private set; }                 // Tooltip/description

    // Organization
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// If true, this attribute is automatically assigned to ALL categories.
    /// Used for universal attributes like weight, dimensions, etc.
    /// </summary>
    public bool IsGlobal { get; private set; }

    // Navigation (for Select/MultiSelect types)
    private readonly List<ProductAttributeValue> _values = new();
    public virtual IReadOnlyCollection<ProductAttributeValue> Values => _values.AsReadOnly();

    // Private constructor for EF Core
    private ProductAttribute() : base() { }

    private ProductAttribute(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new attribute.
    /// </summary>
    public static ProductAttribute Create(
        string code,
        string name,
        AttributeType type,
        string? tenantId = null)
    {
        var attribute = new ProductAttribute(Guid.NewGuid(), tenantId)
        {
            Code = code.ToLowerInvariant().Replace(" ", "_"),
            Name = name,
            Type = type
        };
        attribute.AddDomainEvent(new ProductAttributeCreatedEvent(attribute.Id, code, name, type));
        return attribute;
    }

    /// <summary>
    /// Updates basic attribute details.
    /// </summary>
    public void UpdateDetails(string code, string name)
    {
        Code = code.ToLowerInvariant().Replace(" ", "_");
        Name = name;
        AddDomainEvent(new ProductAttributeUpdatedEvent(Id, code, name));
    }

    /// <summary>
    /// Sets the attribute type. Only allowed if no products are using this attribute.
    /// </summary>
    public void SetType(AttributeType type)
    {
        Type = type;
    }

    /// <summary>
    /// Configures behavior flags for filtering, searching, and variants.
    /// </summary>
    public void SetBehaviorFlags(bool isFilterable, bool isSearchable, bool isRequired, bool isVariantAttribute)
    {
        IsFilterable = isFilterable;
        IsSearchable = isSearchable;
        IsRequired = isRequired;
        IsVariantAttribute = isVariantAttribute;
    }

    /// <summary>
    /// Configures display flags for product cards and specifications.
    /// </summary>
    public void SetDisplayFlags(bool showInProductCard, bool showInSpecifications)
    {
        ShowInProductCard = showInProductCard;
        ShowInSpecifications = showInSpecifications;
    }

    /// <summary>
    /// Sets type-specific configuration for validation and constraints.
    /// </summary>
    public void SetTypeConfiguration(
        string? unit,
        string? validationRegex,
        decimal? minValue,
        decimal? maxValue,
        int? maxLength)
    {
        Unit = unit;
        ValidationRegex = validationRegex;
        MinValue = minValue;
        MaxValue = maxValue;
        MaxLength = maxLength;
    }

    /// <summary>
    /// Sets default value, placeholder, and help text.
    /// </summary>
    public void SetDefaults(string? defaultValue, string? placeholder, string? helpText)
    {
        DefaultValue = defaultValue;
        Placeholder = placeholder;
        HelpText = helpText;
    }

    /// <summary>
    /// Sets the active status.
    /// </summary>
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    /// <summary>
    /// Sets the display order.
    /// </summary>
    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Sets whether this attribute is global (auto-assigned to all categories).
    /// </summary>
    public void SetGlobal(bool isGlobal)
    {
        IsGlobal = isGlobal;
    }

    /// <summary>
    /// Adds a new value to this attribute (for Select/MultiSelect types).
    /// </summary>
    public ProductAttributeValue AddValue(string value, string displayValue, int sortOrder = 0)
    {
        if (Type != AttributeType.Select && Type != AttributeType.MultiSelect)
        {
            throw new InvalidOperationException(
                $"Values can only be added to Select or MultiSelect attributes. [{ErrorCodes.Attribute.ValuesOnlyForSelectTypes}]");
        }

        if (_values.Any(v => v.Value.Equals(value, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Value '{value}' already exists for this attribute. [{ErrorCodes.Attribute.ValueAlreadyExists}]");
        }

        var attributeValue = ProductAttributeValue.Create(Id, value, displayValue, sortOrder, TenantId);
        _values.Add(attributeValue);

        AddDomainEvent(new ProductAttributeValueAddedEvent(Id, attributeValue.Id, value, displayValue));
        return attributeValue;
    }

    /// <summary>
    /// Removes a value from this attribute.
    /// </summary>
    public void RemoveValue(Guid valueId)
    {
        var value = _values.FirstOrDefault(v => v.Id == valueId);
        if (value == null)
        {
            throw new InvalidOperationException(
                $"Value with ID '{valueId}' not found. [{ErrorCodes.Attribute.ValueNotFound}]");
        }

        _values.Remove(value);
        AddDomainEvent(new ProductAttributeValueRemovedEvent(Id, valueId));
    }

    /// <summary>
    /// Gets a value by ID.
    /// </summary>
    public ProductAttributeValue? GetValue(Guid valueId)
    {
        return _values.FirstOrDefault(v => v.Id == valueId);
    }

    /// <summary>
    /// Marks the attribute for deletion.
    /// </summary>
    public void MarkAsDeleted()
    {
        AddDomainEvent(new ProductAttributeDeletedEvent(Id));
    }

    /// <summary>
    /// Returns true if this attribute type requires predefined values.
    /// </summary>
    public bool RequiresValues => Type == AttributeType.Select || Type == AttributeType.MultiSelect;
}
