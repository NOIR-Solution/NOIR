namespace NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeFormSchema;

/// <summary>
/// Query to get the form schema for a product's attributes.
/// Returns all attributes applicable to the product (based on category) with current values.
/// </summary>
public sealed record GetProductAttributeFormSchemaQuery(
    Guid ProductId,
    Guid? VariantId = null);
