namespace NOIR.Application.Features.ProductAttributes.DTOs;

/// <summary>
/// Full product attribute details DTO.
/// </summary>
public sealed record ProductAttributeDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    bool IsFilterable,
    bool IsSearchable,
    bool IsRequired,
    bool IsVariantAttribute,
    bool ShowInProductCard,
    bool ShowInSpecifications,
    bool IsGlobal,
    string? Unit,
    string? ValidationRegex,
    decimal? MinValue,
    decimal? MaxValue,
    int? MaxLength,
    string? DefaultValue,
    string? Placeholder,
    string? HelpText,
    int SortOrder,
    bool IsActive,
    IReadOnlyCollection<ProductAttributeValueDto> Values,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Product attribute list item DTO (minimal data for listings).
/// </summary>
public sealed record ProductAttributeListDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    bool IsFilterable,
    bool IsVariantAttribute,
    bool IsGlobal,
    bool IsActive,
    int ValueCount);

/// <summary>
/// Product attribute value DTO.
/// </summary>
public sealed record ProductAttributeValueDto(
    Guid Id,
    string Value,
    string DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    string? IconUrl,
    int SortOrder,
    bool IsActive,
    int ProductCount);

/// <summary>
/// Request to create a new product attribute.
/// </summary>
public sealed record CreateProductAttributeRequest(
    string Code,
    string Name,
    string Type,
    bool IsFilterable = false,
    bool IsSearchable = false,
    bool IsRequired = false,
    bool IsVariantAttribute = false,
    bool ShowInProductCard = false,
    bool ShowInSpecifications = true,
    string? Unit = null,
    string? ValidationRegex = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    int? MaxLength = null,
    string? DefaultValue = null,
    string? Placeholder = null,
    string? HelpText = null);

/// <summary>
/// Request to update an existing product attribute.
/// </summary>
public sealed record UpdateProductAttributeRequest(
    string Code,
    string Name,
    bool IsFilterable,
    bool IsSearchable,
    bool IsRequired,
    bool IsVariantAttribute,
    bool ShowInProductCard,
    bool ShowInSpecifications,
    string? Unit,
    string? ValidationRegex,
    decimal? MinValue,
    decimal? MaxValue,
    int? MaxLength,
    string? DefaultValue,
    string? Placeholder,
    string? HelpText,
    int SortOrder,
    bool IsActive);

/// <summary>
/// Request to add a value to a product attribute.
/// </summary>
public sealed record AddProductAttributeValueRequest(
    string Value,
    string DisplayValue,
    string? ColorCode = null,
    string? SwatchUrl = null,
    string? IconUrl = null,
    int SortOrder = 0);

/// <summary>
/// Request to update a product attribute value.
/// </summary>
public sealed record UpdateProductAttributeValueRequest(
    string Value,
    string DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    string? IconUrl,
    int SortOrder,
    bool IsActive);

/// <summary>
/// Category attribute link DTO.
/// </summary>
public sealed record CategoryAttributeDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    Guid AttributeId,
    string AttributeName,
    string AttributeCode,
    bool IsRequired,
    int SortOrder);

/// <summary>
/// Request to link an attribute to a category.
/// </summary>
public sealed record LinkCategoryAttributeRequest(
    Guid CategoryId,
    Guid AttributeId,
    bool IsRequired = false,
    int SortOrder = 0);

// ============================================================================
// Product Attribute Assignment DTOs (Phase 4)
// ============================================================================

/// <summary>
/// Product attribute assignment DTO (stores a product's attribute value).
/// </summary>
public sealed record ProductAttributeAssignmentDto(
    Guid Id,
    Guid ProductId,
    Guid AttributeId,
    string AttributeCode,
    string AttributeName,
    string AttributeType,
    Guid? VariantId,
    object? Value,
    string? DisplayValue,
    bool IsRequired);

/// <summary>
/// Form schema for a product's attributes (used to render dynamic form).
/// </summary>
public sealed record ProductAttributeFormSchemaDto(
    Guid ProductId,
    string ProductName,
    Guid? CategoryId,
    string? CategoryName,
    IReadOnlyCollection<ProductAttributeFormFieldDto> Fields);

/// <summary>
/// Individual form field for a product attribute.
/// </summary>
public sealed record ProductAttributeFormFieldDto(
    Guid AttributeId,
    string Code,
    string Name,
    string Type,
    bool IsRequired,
    string? Unit,
    string? Placeholder,
    string? HelpText,
    decimal? MinValue,
    decimal? MaxValue,
    int? MaxLength,
    string? DefaultValue,
    string? ValidationRegex,
    IReadOnlyCollection<ProductAttributeValueDto>? Options,
    object? CurrentValue,
    string? CurrentDisplayValue);

/// <summary>
/// Request to set a single attribute value for a product.
/// </summary>
public sealed record SetProductAttributeValueRequest(
    Guid AttributeId,
    Guid? VariantId,
    object? Value);

/// <summary>
/// Request to bulk update multiple attribute values for a product.
/// </summary>
public sealed record BulkUpdateProductAttributesRequest(
    Guid? VariantId,
    IReadOnlyCollection<AttributeValueItem> Values);

/// <summary>
/// Individual attribute value item for bulk update.
/// </summary>
public sealed record AttributeValueItem(
    Guid AttributeId,
    object? Value);
