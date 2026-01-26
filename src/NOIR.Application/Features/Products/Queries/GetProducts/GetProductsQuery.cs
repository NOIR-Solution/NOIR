namespace NOIR.Application.Features.Products.Queries.GetProducts;

/// <summary>
/// Query to get a list of products with optional filtering and pagination.
/// </summary>
public sealed record GetProductsQuery(
    string? Search = null,
    ProductStatus? Status = null,
    Guid? CategoryId = null,
    string? Brand = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? InStockOnly = null,
    int Page = 1,
    int PageSize = 20);
