namespace NOIR.Application.Features.Products.Queries.GetProductById;

/// <summary>
/// Query to get a single product by ID or slug.
/// </summary>
public sealed record GetProductByIdQuery(
    Guid? Id = null,
    string? Slug = null);
