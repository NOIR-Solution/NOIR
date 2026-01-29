namespace NOIR.Application.Features.ProductAttributes;

/// <summary>
/// Mapper for ProductAttribute entity to DTOs.
/// </summary>
public static class ProductAttributeMapper
{
    /// <summary>
    /// Maps a ProductAttribute entity to a full ProductAttributeDto.
    /// </summary>
    public static ProductAttributeDto ToDto(ProductAttribute attribute) => new(
        attribute.Id,
        attribute.Code,
        attribute.Name,
        attribute.Type.ToString(),
        attribute.IsFilterable,
        attribute.IsSearchable,
        attribute.IsRequired,
        attribute.IsVariantAttribute,
        attribute.ShowInProductCard,
        attribute.ShowInSpecifications,
        attribute.IsGlobal,
        attribute.Unit,
        attribute.ValidationRegex,
        attribute.MinValue,
        attribute.MaxValue,
        attribute.MaxLength,
        attribute.DefaultValue,
        attribute.Placeholder,
        attribute.HelpText,
        attribute.SortOrder,
        attribute.IsActive,
        attribute.Values.Select(ToValueDto).ToList(),
        attribute.CreatedAt,
        attribute.ModifiedAt);

    /// <summary>
    /// Maps a ProductAttribute entity to a ProductAttributeListDto.
    /// </summary>
    public static ProductAttributeListDto ToListDto(ProductAttribute attribute) => new(
        attribute.Id,
        attribute.Code,
        attribute.Name,
        attribute.Type.ToString(),
        attribute.IsFilterable,
        attribute.IsVariantAttribute,
        attribute.IsGlobal,
        attribute.IsActive,
        attribute.Values.Count);

    /// <summary>
    /// Maps a ProductAttributeValue entity to a ProductAttributeValueDto.
    /// </summary>
    public static ProductAttributeValueDto ToValueDto(ProductAttributeValue value) => new(
        value.Id,
        value.Value,
        value.DisplayValue,
        value.ColorCode,
        value.SwatchUrl,
        value.IconUrl,
        value.SortOrder,
        value.IsActive,
        value.ProductCount);

    /// <summary>
    /// Maps a CategoryAttribute entity to a CategoryAttributeDto.
    /// </summary>
    public static CategoryAttributeDto ToCategoryAttributeDto(CategoryAttribute ca) => new(
        ca.Id,
        ca.CategoryId,
        ca.Category?.Name ?? string.Empty,
        ca.AttributeId,
        ca.Attribute?.Name ?? string.Empty,
        ca.Attribute?.Code ?? string.Empty,
        ca.IsRequired,
        ca.SortOrder);
}
