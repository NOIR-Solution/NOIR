namespace NOIR.Domain.Entities;

/// <summary>
/// Category for organizing blog posts.
/// </summary>
public class PostCategory : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// Category display name.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// URL-friendly slug for the category.
    /// </summary>
    public string Slug { get; private set; } = default!;

    /// <summary>
    /// Optional description of the category.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Parent category ID for hierarchical categories.
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Navigation property to parent category.
    /// </summary>
    public PostCategory? Parent { get; private set; }

    /// <summary>
    /// Navigation property to child categories.
    /// </summary>
    public ICollection<PostCategory> Children { get; private set; } = new List<PostCategory>();

    /// <summary>
    /// Sort order for display.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// SEO meta title for category page.
    /// </summary>
    public string? MetaTitle { get; private set; }

    /// <summary>
    /// SEO meta description for category page.
    /// </summary>
    public string? MetaDescription { get; private set; }

    /// <summary>
    /// Category image URL for category listings.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Number of posts in this category (denormalized for performance).
    /// </summary>
    public int PostCount { get; private set; }

    /// <summary>
    /// Navigation property to posts in this category.
    /// </summary>
    public ICollection<Post> Posts { get; private set; } = new List<Post>();

    // Private constructor for EF Core
    private PostCategory() : base() { }

    /// <summary>
    /// Creates a new post category.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static PostCategory Create(
        string name,
        string slug,
        string? description = null,
        Guid? parentId = null,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new PostCategory
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            Description = description,
            ParentId = parentId,
            SortOrder = 0,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Updates the category details.
    /// </summary>
    public void Update(
        string name,
        string slug,
        string? description = null,
        Guid? parentId = null)
    {
        Name = name;
        Slug = slug.ToLowerInvariant();
        Description = description;
        ParentId = parentId;
    }

    /// <summary>
    /// Updates SEO metadata.
    /// </summary>
    public void UpdateSeo(string? metaTitle, string? metaDescription)
    {
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
    }

    /// <summary>
    /// Updates the category image.
    /// </summary>
    public void UpdateImage(string? imageUrl)
    {
        ImageUrl = imageUrl;
    }

    /// <summary>
    /// Updates the sort order.
    /// </summary>
    public void SetSortOrder(int order)
    {
        SortOrder = order;
    }

    /// <summary>
    /// Updates the parent category.
    /// </summary>
    public void SetParent(Guid? parentId)
    {
        ParentId = parentId;
    }

    /// <summary>
    /// Increments the post count.
    /// </summary>
    public void IncrementPostCount()
    {
        PostCount++;
    }

    /// <summary>
    /// Decrements the post count.
    /// </summary>
    public void DecrementPostCount()
    {
        if (PostCount > 0)
            PostCount--;
    }
}
