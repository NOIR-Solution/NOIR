namespace NOIR.Application.Features.Blog.Queries.GetCategories;

/// <summary>
/// Query to get a list of blog categories.
/// </summary>
public sealed record GetCategoriesQuery(
    string? Search = null,
    bool TopLevelOnly = false,
    bool IncludeChildren = false);
