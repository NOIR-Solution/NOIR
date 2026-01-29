namespace NOIR.Application.Features.FilterAnalytics.Queries.GetPopularFilters;

/// <summary>
/// Query to get the most popular filters by usage count.
/// </summary>
public sealed record GetPopularFiltersQuery(
    DateTimeOffset? FromDate = null,
    DateTimeOffset? ToDate = null,
    string? CategorySlug = null,
    int Top = 20);
