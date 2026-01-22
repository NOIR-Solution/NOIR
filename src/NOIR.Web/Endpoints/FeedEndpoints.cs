using NOIR.Application.Features.Blog.Queries.GetRssFeed;
using NOIR.Application.Features.Blog.Queries.GetSitemap;

namespace NOIR.Web.Endpoints;

/// <summary>
/// SEO feed endpoints for blog.
/// Provides RSS feed and XML sitemap for search engines.
/// Note: Default tenant context for anonymous access is set by FeedTenantMiddleware.
/// </summary>
public static class FeedEndpoints
{
    public static void MapFeedEndpoints(this IEndpointRouteBuilder app)
    {
        // RSS Feed - public access
        app.MapGet("/blog/feed.xml", async (
            [FromQuery] int? maxItems,
            [FromQuery] Guid? categoryId,
            IMessageBus bus) =>
        {
            var query = new GetRssFeedQuery(maxItems ?? 20, categoryId);
            var result = await bus.InvokeAsync<Result<string>>(query);

            if (result.IsFailure)
            {
                return Results.Problem(result.Error.Message);
            }

            return Results.Content(result.Value, "application/rss+xml; charset=utf-8");
        })
        .AllowAnonymous()
        .WithName("GetRssFeed")
        .WithTags("Blog Feeds")
        .WithSummary("Get RSS 2.0 feed of published blog posts")
        .WithDescription("Returns an RSS 2.0 feed of the most recent published blog posts. Supports filtering by category.")
        .Produces<string>(StatusCodes.Status200OK, "application/rss+xml")
        .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));

        // Alternative RSS path for compatibility
        app.MapGet("/rss.xml", async (IMessageBus bus) =>
        {
            var query = new GetRssFeedQuery();
            var result = await bus.InvokeAsync<Result<string>>(query);

            if (result.IsFailure)
            {
                return Results.Problem(result.Error.Message);
            }

            return Results.Content(result.Value, "application/rss+xml; charset=utf-8");
        })
        .AllowAnonymous()
        .WithName("GetRssFeedAlt")
        .WithTags("Blog Feeds")
        .WithSummary("Get RSS feed (alternative path)")
        .ExcludeFromDescription()
        .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));

        // XML Sitemap - public access
        app.MapGet("/sitemap.xml", async (
            [FromQuery] bool? includeImages,
            IMessageBus bus) =>
        {
            var query = new GetSitemapQuery(includeImages ?? true);
            var result = await bus.InvokeAsync<Result<string>>(query);

            if (result.IsFailure)
            {
                return Results.Problem(result.Error.Message);
            }

            return Results.Content(result.Value, "application/xml; charset=utf-8");
        })
        .AllowAnonymous()
        .WithName("GetSitemap")
        .WithTags("Blog Feeds")
        .WithSummary("Get XML sitemap for search engines")
        .WithDescription("Returns an XML sitemap with all published blog posts and categories. Includes image references for image SEO.")
        .Produces<string>(StatusCodes.Status200OK, "application/xml")
        .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(30)));

        // robots.txt - include sitemap reference
        app.MapGet("/robots.txt", () =>
        {
            var content = """
                User-agent: *
                Allow: /

                Sitemap: /sitemap.xml
                """;

            return Results.Content(content, "text/plain; charset=utf-8");
        })
        .AllowAnonymous()
        .WithName("GetRobotsTxt")
        .WithTags("Blog Feeds")
        .WithSummary("Get robots.txt with sitemap reference")
        .ExcludeFromDescription()
        .CacheOutput(policy => policy.Expire(TimeSpan.FromHours(24)));
    }
}
