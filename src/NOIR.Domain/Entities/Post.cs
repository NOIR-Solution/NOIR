namespace NOIR.Domain.Entities;

/// <summary>
/// Blog post entity for the CMS feature.
/// Stores content in both BlockNote JSON format and rendered HTML.
/// </summary>
public class Post : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// Post title displayed in lists and header.
    /// </summary>
    public string Title { get; private set; } = default!;

    /// <summary>
    /// URL-friendly slug for the post (e.g., "my-first-post").
    /// </summary>
    public string Slug { get; private set; } = default!;

    /// <summary>
    /// Short summary/excerpt of the post for previews and SEO.
    /// </summary>
    public string? Excerpt { get; private set; }

    /// <summary>
    /// Raw BlockNote editor content stored as JSON.
    /// Used for editing and re-rendering.
    /// </summary>
    public string? ContentJson { get; private set; }

    /// <summary>
    /// Rendered HTML content for display.
    /// Generated from ContentJson for performance.
    /// </summary>
    public string? ContentHtml { get; private set; }

    /// <summary>
    /// Featured image URL for post cards and headers.
    /// Kept for backward compatibility - prefer using FeaturedImageId.
    /// </summary>
    public string? FeaturedImageUrl { get; private set; }

    /// <summary>
    /// Alt text for featured image (accessibility and SEO).
    /// </summary>
    public string? FeaturedImageAlt { get; private set; }

    /// <summary>
    /// Reference to the MediaFile entity for the featured image.
    /// Provides access to responsive variants, placeholders, and metadata.
    /// </summary>
    public Guid? FeaturedImageId { get; private set; }

    /// <summary>
    /// Navigation property to the featured image MediaFile.
    /// </summary>
    public MediaFile? FeaturedImage { get; private set; }

    /// <summary>
    /// Publication status of the post.
    /// </summary>
    public PostStatus Status { get; private set; }

    /// <summary>
    /// When the post was/will be published.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; private set; }

    /// <summary>
    /// Scheduled publication date for future posts.
    /// </summary>
    public DateTimeOffset? ScheduledPublishAt { get; private set; }

    /// <summary>
    /// SEO meta title (defaults to Title if not set).
    /// </summary>
    public string? MetaTitle { get; private set; }

    /// <summary>
    /// SEO meta description for search results.
    /// </summary>
    public string? MetaDescription { get; private set; }

    /// <summary>
    /// Canonical URL if different from default.
    /// </summary>
    public string? CanonicalUrl { get; private set; }

    /// <summary>
    /// Whether search engines should index this post.
    /// </summary>
    public bool AllowIndexing { get; private set; } = true;

    /// <summary>
    /// Category ID this post belongs to.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// Navigation property to category.
    /// </summary>
    public PostCategory? Category { get; private set; }

    /// <summary>
    /// Author user ID.
    /// </summary>
    public Guid AuthorId { get; private set; }

    /// <summary>
    /// Number of times the post has been viewed.
    /// </summary>
    public long ViewCount { get; private set; }

    /// <summary>
    /// Estimated reading time in minutes.
    /// </summary>
    public int ReadingTimeMinutes { get; private set; }

    /// <summary>
    /// Navigation property to tag assignments (many-to-many).
    /// </summary>
    public ICollection<PostTagAssignment> TagAssignments { get; private set; } = new List<PostTagAssignment>();

    // Private constructor for EF Core
    private Post() : base() { }

    /// <summary>
    /// Creates a new blog post as a draft.
    /// </summary>
    public static Post Create(
        string title,
        string slug,
        Guid authorId,
        string? tenantId = null)
    {
        return new Post
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug.ToLowerInvariant(),
            AuthorId = authorId,
            Status = PostStatus.Draft,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Updates the post's basic content.
    /// </summary>
    public void UpdateContent(
        string title,
        string slug,
        string? excerpt,
        string? contentJson,
        string? contentHtml)
    {
        Title = title;
        Slug = slug.ToLowerInvariant();
        Excerpt = excerpt;
        ContentJson = contentJson;
        ContentHtml = contentHtml;
        ReadingTimeMinutes = CalculateReadingTime(contentHtml);
    }

    /// <summary>
    /// Updates the post's featured image using a MediaFile reference.
    /// This is the preferred method as it provides access to all image metadata.
    /// </summary>
    public void SetFeaturedImage(Guid? mediaFileId, string? altText = null)
    {
        FeaturedImageId = mediaFileId;
        FeaturedImageAlt = altText;
        // Clear the URL - it will be populated from the MediaFile when needed
        FeaturedImageUrl = null;
    }

    /// <summary>
    /// Updates the post's featured image using a URL directly.
    /// For backward compatibility - prefer SetFeaturedImage with MediaFile.
    /// </summary>
    public void UpdateFeaturedImage(string? imageUrl, string? altText = null)
    {
        FeaturedImageUrl = imageUrl;
        FeaturedImageAlt = altText;
        // Clear the MediaFile reference when using direct URL
        FeaturedImageId = null;
    }

    /// <summary>
    /// Updates SEO metadata.
    /// </summary>
    public void UpdateSeo(
        string? metaTitle,
        string? metaDescription,
        string? canonicalUrl,
        bool allowIndexing)
    {
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
        CanonicalUrl = canonicalUrl;
        AllowIndexing = allowIndexing;
    }

    /// <summary>
    /// Sets the post category.
    /// </summary>
    public void SetCategory(Guid? categoryId)
    {
        CategoryId = categoryId;
    }

    /// <summary>
    /// Publishes the post immediately.
    /// </summary>
    public void Publish()
    {
        Status = PostStatus.Published;
        PublishedAt = DateTimeOffset.UtcNow;
        ScheduledPublishAt = null;
    }

    /// <summary>
    /// Schedules the post for future publication.
    /// </summary>
    public void Schedule(DateTimeOffset publishAt)
    {
        Status = PostStatus.Scheduled;
        ScheduledPublishAt = publishAt;
        PublishedAt = null;
    }

    /// <summary>
    /// Converts the post back to draft status.
    /// </summary>
    public void Unpublish()
    {
        Status = PostStatus.Draft;
        PublishedAt = null;
        ScheduledPublishAt = null;
    }

    /// <summary>
    /// Archives the post.
    /// </summary>
    public void Archive()
    {
        Status = PostStatus.Archived;
    }

    /// <summary>
    /// Increments the view count.
    /// </summary>
    public void IncrementViewCount()
    {
        ViewCount++;
    }

    /// <summary>
    /// Calculates estimated reading time based on word count.
    /// Average reading speed: 200 words per minute.
    /// </summary>
    private static int CalculateReadingTime(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return 1;

        // Strip HTML tags and count words
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
    }
}
