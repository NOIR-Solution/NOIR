namespace NOIR.Application.Features.Brands.Queries.GetBrands;

/// <summary>
/// Query to get paged list of brands.
/// </summary>
public sealed record GetBrandsQuery(
    string? Search = null,
    bool? IsActive = null,
    bool? IsFeatured = null,
    int Page = 1,
    int PageSize = 20);
