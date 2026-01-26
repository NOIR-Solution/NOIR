namespace NOIR.Application.Features.Products.Queries.GetProductCategoryById;

/// <summary>
/// Query to get a single product category by ID.
/// Used as a before-state resolver for auditing.
/// </summary>
public sealed record GetProductCategoryByIdQuery(Guid Id);
