namespace NOIR.Application.Features.Blog.Queries.GetSitemap;

/// <summary>
/// Query to generate XML sitemap for blog posts and categories.
/// </summary>
public sealed record GetSitemapQuery(
    bool IncludeImages = true);
