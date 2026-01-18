namespace NOIR.Application.Features.Blog.Queries.GetTags;

/// <summary>
/// Query to get a list of blog tags.
/// </summary>
public sealed record GetTagsQuery(string? Search = null);
