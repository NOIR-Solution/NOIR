namespace NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributeAssignments;

/// <summary>
/// Query to get all attribute assignments for a product.
/// </summary>
public sealed record GetProductAttributeAssignmentsQuery(
    Guid ProductId,
    Guid? VariantId = null);
