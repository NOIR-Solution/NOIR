namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Attribute types for product characteristics.
/// Supports various input types for flexible product specifications.
/// </summary>
public enum AttributeType
{
    /// <summary>Single choice dropdown (requires predefined values).</summary>
    Select = 0,

    /// <summary>Multiple choice checkboxes (requires predefined values).</summary>
    MultiSelect = 1,

    /// <summary>Free text input (single line).</summary>
    Text = 2,

    /// <summary>Multi-line text input.</summary>
    TextArea = 3,

    /// <summary>Integer number input.</summary>
    Number = 4,

    /// <summary>Decimal number input.</summary>
    Decimal = 5,

    /// <summary>Boolean toggle switch.</summary>
    Boolean = 6,

    /// <summary>Date picker (date only).</summary>
    Date = 7,

    /// <summary>DateTime picker (date and time).</summary>
    DateTime = 8,

    /// <summary>Color picker (outputs hex code).</summary>
    Color = 9,

    /// <summary>Min/Max range input.</summary>
    Range = 10,

    /// <summary>URL input with validation.</summary>
    Url = 11,

    /// <summary>File upload reference.</summary>
    File = 12
}
