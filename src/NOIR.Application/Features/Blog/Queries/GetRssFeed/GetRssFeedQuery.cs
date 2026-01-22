namespace NOIR.Application.Features.Blog.Queries.GetRssFeed;

/// <summary>
/// Query to generate RSS 2.0 feed for published blog posts.
/// </summary>
public sealed record GetRssFeedQuery(
    int MaxItems = 20,
    Guid? CategoryId = null);
