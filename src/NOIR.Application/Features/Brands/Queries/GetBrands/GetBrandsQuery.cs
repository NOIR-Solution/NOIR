namespace NOIR.Application.Features.Brands.Queries.GetBrands;

/// <summary>
/// Query to get paged list of brands.
/// </summary>
public sealed record GetBrandsQuery(
    string? Search = null,
    bool? IsActive = null,
    bool? IsFeatured = null,
    int PageNumber = 1,
    int PageSize = 20);
