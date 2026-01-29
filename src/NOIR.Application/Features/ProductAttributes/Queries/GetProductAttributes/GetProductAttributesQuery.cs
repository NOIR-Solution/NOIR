namespace NOIR.Application.Features.ProductAttributes.Queries.GetProductAttributes;

/// <summary>
/// Query to get paged list of product attributes.
/// </summary>
public sealed record GetProductAttributesQuery(
    string? Search = null,
    bool? IsActive = null,
    bool? IsFilterable = null,
    bool? IsVariantAttribute = null,
    string? Type = null,
    int PageNumber = 1,
    int PageSize = 20);
