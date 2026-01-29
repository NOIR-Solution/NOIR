namespace NOIR.Application.Features.ProductAttributes.Queries.GetCategoryAttributes;

/// <summary>
/// Query to get all attributes assigned to a category.
/// </summary>
public sealed record GetCategoryAttributesQuery(Guid CategoryId);
