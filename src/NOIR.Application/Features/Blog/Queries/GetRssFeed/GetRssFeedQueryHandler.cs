using System.Xml;
using NOIR.Application.Features.Blog.Helpers;

namespace NOIR.Application.Features.Blog.Queries.GetRssFeed;

/// <summary>
/// Wolverine handler for generating RSS 2.0 feed.
/// Returns XML string with published blog posts.
/// Note: HTTP output caching is handled at the endpoint level.
/// </summary>
public class GetRssFeedQueryHandler
{
    private readonly IRepository<Post, Guid> _postRepository;

    public GetRssFeedQueryHandler(IRepository<Post, Guid> postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<Result<string>> Handle(
        GetRssFeedQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new PublishedPostsSpec(
            search: null,
            categoryId: query.CategoryId,
            tagId: null,
            skip: 0,
            take: query.MaxItems);

        var posts = await _postRepository.ListAsync(spec, cancellationToken);

        var xml = GenerateRssFeed(posts);

        return Result.Success(xml);
    }

    private static string GenerateRssFeed(IEnumerable<Post> posts)
    {
        var sb = new StringBuilder();
        using var writer = XmlWriter.Create(sb, new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        });

        writer.WriteStartDocument();
        writer.WriteStartElement("rss");
        writer.WriteAttributeString("version", "2.0");
        writer.WriteAttributeString("xmlns", "atom", null, "http://www.w3.org/2005/Atom");
        writer.WriteAttributeString("xmlns", "media", null, "http://search.yahoo.com/mrss/");

        writer.WriteStartElement("channel");

        // Channel metadata
        writer.WriteElementString("title", "Blog");
        writer.WriteElementString("description", "Latest blog posts");
        writer.WriteElementString("link", "/blog");
        writer.WriteElementString("language", "en-us");
        writer.WriteElementString("lastBuildDate", DateTimeOffset.UtcNow.ToString("R"));
        writer.WriteElementString("generator", "NOIR CMS");

        // Atom self-link (required for valid RSS)
        writer.WriteStartElement("atom", "link", "http://www.w3.org/2005/Atom");
        writer.WriteAttributeString("href", "/blog/feed.xml");
        writer.WriteAttributeString("rel", "self");
        writer.WriteAttributeString("type", "application/rss+xml");
        writer.WriteEndElement();

        // Write items
        foreach (var post in posts)
        {
            WritePostItem(writer, post);
        }

        writer.WriteEndElement(); // channel
        writer.WriteEndElement(); // rss
        writer.WriteEndDocument();
        writer.Flush();

        return sb.ToString();
    }

    private static void WritePostItem(XmlWriter writer, Post post)
    {
        writer.WriteStartElement("item");

        // Use effective meta title (custom or auto-generated from post title)
        var title = SeoHelpers.GetEffectiveMetaTitle(post.MetaTitle, post.Title);
        writer.WriteElementString("title", title);
        writer.WriteElementString("link", $"/blog/{post.Slug}");
        writer.WriteElementString("guid", $"/blog/{post.Slug}");

        // Use effective meta description (custom or auto-generated from excerpt/content)
        var description = SeoHelpers.GetEffectiveMetaDescription(
            post.MetaDescription,
            post.Excerpt,
            post.ContentHtml);
        if (!string.IsNullOrEmpty(description))
        {
            writer.WriteElementString("description", description);
        }

        if (post.PublishedAt.HasValue)
        {
            writer.WriteElementString("pubDate", post.PublishedAt.Value.ToString("R"));
        }

        if (post.Category != null)
        {
            writer.WriteElementString("category", post.Category.Name);
        }

        // Media content for featured image
        var imageUrl = post.FeaturedImage?.DefaultUrl ?? post.FeaturedImageUrl;
        if (!string.IsNullOrEmpty(imageUrl))
        {
            writer.WriteStartElement("media", "content", "http://search.yahoo.com/mrss/");
            writer.WriteAttributeString("url", imageUrl);
            writer.WriteAttributeString("medium", "image");
            if (!string.IsNullOrEmpty(post.FeaturedImageAlt))
            {
                writer.WriteAttributeString("title", post.FeaturedImageAlt);
            }
            writer.WriteEndElement();
        }

        writer.WriteEndElement(); // item
    }
}
