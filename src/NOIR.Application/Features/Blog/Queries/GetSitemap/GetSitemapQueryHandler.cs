using System.Xml;

namespace NOIR.Application.Features.Blog.Queries.GetSitemap;

/// <summary>
/// Wolverine handler for generating XML sitemap.
/// Includes published posts and categories with image references.
/// Note: HTTP output caching is handled at the endpoint level.
/// </summary>
public class GetSitemapQueryHandler
{
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IRepository<PostCategory, Guid> _categoryRepository;

    private const string SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private const string ImageNamespace = "http://www.google.com/schemas/sitemap-image/1.1";

    public GetSitemapQueryHandler(
        IRepository<Post, Guid> postRepository,
        IRepository<PostCategory, Guid> categoryRepository)
    {
        _postRepository = postRepository;
        _categoryRepository = categoryRepository;
    }

    public async Task<Result<string>> Handle(
        GetSitemapQuery query,
        CancellationToken cancellationToken)
    {
        // Get all published posts (no pagination for sitemap)
        var postSpec = new PublishedPostsSpec(
            search: null,
            categoryId: null,
            tagId: null,
            skip: null,
            take: null);

        var posts = await _postRepository.ListAsync(postSpec, cancellationToken);

        // Get all categories
        var categorySpec = new ActiveCategoriesSpec();
        var categories = await _categoryRepository.ListAsync(categorySpec, cancellationToken);

        var xml = GenerateSitemap(posts, categories, query.IncludeImages);

        return Result.Success(xml);
    }

    private static string GenerateSitemap(
        IEnumerable<Post> posts,
        IEnumerable<PostCategory> categories,
        bool includeImages)
    {
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        });

        writer.WriteStartDocument();
        writer.WriteStartElement("urlset", SitemapNamespace);

        if (includeImages)
        {
            writer.WriteAttributeString("xmlns", "image", null, ImageNamespace);
        }

        // Add blog index page
        WriteUrl(writer, "/blog", DateTimeOffset.UtcNow, "daily", "0.9", includeImages, null, null);

        // Add posts
        foreach (var post in posts)
        {
            var imageUrl = post.FeaturedImage?.DefaultUrl ?? post.FeaturedImageUrl;
            var imageAlt = post.FeaturedImageAlt;

            WriteUrl(
                writer,
                $"/blog/{post.Slug}",
                post.ModifiedAt ?? post.PublishedAt ?? post.CreatedAt,
                "weekly",
                "0.8",
                includeImages,
                imageUrl,
                imageAlt);
        }

        // Add categories
        foreach (var category in categories)
        {
            WriteUrl(
                writer,
                $"/blog/category/{category.Slug}",
                category.ModifiedAt ?? category.CreatedAt,
                "weekly",
                "0.6",
                includeImages,
                category.ImageUrl,
                category.Name);
        }

        writer.WriteEndElement(); // urlset
        writer.WriteEndDocument();
        writer.Flush();

        return sb.ToString();
    }

    private static void WriteUrl(
        XmlWriter writer,
        string loc,
        DateTimeOffset lastmod,
        string changefreq,
        string priority,
        bool includeImages,
        string? imageUrl,
        string? imageTitle)
    {
        writer.WriteStartElement("url");

        writer.WriteElementString("loc", loc);
        writer.WriteElementString("lastmod", lastmod.ToString("yyyy-MM-dd"));
        writer.WriteElementString("changefreq", changefreq);
        writer.WriteElementString("priority", priority);

        // Add image reference if available
        if (includeImages && !string.IsNullOrEmpty(imageUrl))
        {
            writer.WriteStartElement("image", "image", ImageNamespace);
            writer.WriteElementString("image", "loc", ImageNamespace, imageUrl);
            if (!string.IsNullOrEmpty(imageTitle))
            {
                writer.WriteElementString("image", "title", ImageNamespace, imageTitle);
            }
            writer.WriteEndElement();
        }

        writer.WriteEndElement(); // url
    }
}
