namespace NOIR.Application.Features.ProductFilter.Queries.GetCategoryFilters;

/// <summary>
/// Query to get available filters for a specific category.
/// Returns filter definitions with predefined values.
/// </summary>
public sealed record GetCategoryFiltersQuery(string CategorySlug);
