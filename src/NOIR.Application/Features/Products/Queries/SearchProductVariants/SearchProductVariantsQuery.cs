namespace NOIR.Application.Features.Products.Queries.SearchProductVariants;

/// <summary>
/// Query to search active product variants for selection (e.g., manual order creation).
/// </summary>
public sealed record SearchProductVariantsQuery(
    string? Search = null,
    Guid? CategoryId = null,
    int Page = 1,
    int PageSize = 20);
