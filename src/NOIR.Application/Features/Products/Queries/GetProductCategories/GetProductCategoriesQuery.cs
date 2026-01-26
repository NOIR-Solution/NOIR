namespace NOIR.Application.Features.Products.Queries.GetProductCategories;

/// <summary>
/// Query to get a list of product categories.
/// </summary>
public sealed record GetProductCategoriesQuery(
    string? Search = null,
    bool TopLevelOnly = false,
    bool IncludeChildren = false);
