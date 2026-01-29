namespace NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributeFormSchema;

/// <summary>
/// Query to get the form schema for a category's attributes.
/// Used for new product creation - returns all attributes applicable to the category with default values.
/// Unlike GetProductAttributeFormSchema, this does NOT require a productId.
/// </summary>
public sealed record GetCategoryAttributeFormSchemaQuery(Guid CategoryId);
