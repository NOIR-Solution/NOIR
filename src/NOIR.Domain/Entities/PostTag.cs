namespace NOIR.Domain.Entities;

/// <summary>
/// Tag for categorizing and filtering blog posts.
/// Tags are tenant-specific and can be applied to multiple posts.
/// </summary>
public class PostTag : TenantAggregateRoot<Guid>
{
    /// <summary>
    /// Tag display name.
    /// </summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// URL-friendly slug for the tag.
    /// </summary>
    public string Slug { get; private set; } = default!;

    /// <summary>
    /// Optional description of the tag.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Color for visual display (hex format, e.g., "#3B82F6").
    /// </summary>
    public string? Color { get; private set; }

    /// <summary>
    /// Number of posts using this tag (denormalized for performance).
    /// </summary>
    public int PostCount { get; private set; }

    /// <summary>
    /// Navigation property to posts via join entity.
    /// </summary>
    public ICollection<PostTagAssignment> PostAssignments { get; private set; } = new List<PostTagAssignment>();

    // Private constructor for EF Core
    private PostTag() : base() { }

    /// <summary>
    /// Creates a new post tag.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static PostTag Create(
        string name,
        string slug,
        string? description = null,
        string? color = null,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new PostTag
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            Description = description,
            Color = color,
            TenantId = tenantId
        };
    }

    /// <summary>
    /// Updates the tag details.
    /// </summary>
    public void Update(
        string name,
        string slug,
        string? description = null,
        string? color = null)
    {
        Name = name;
        Slug = slug.ToLowerInvariant();
        Description = description;
        Color = color;
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

/// <summary>
/// Join entity for many-to-many relationship between Posts and Tags.
/// </summary>
public class PostTagAssignment : Entity<Guid>, ITenantEntity
{
    /// <summary>
    /// Post ID.
    /// </summary>
    public Guid PostId { get; private set; }

    /// <summary>
    /// Navigation property to post.
    /// </summary>
    public Post? Post { get; private set; }

    /// <summary>
    /// Tag ID.
    /// </summary>
    public Guid TagId { get; private set; }

    /// <summary>
    /// Navigation property to tag.
    /// </summary>
    public PostTag? Tag { get; private set; }

    /// <summary>
    /// Tenant ID for multi-tenancy.
    /// </summary>
    public string? TenantId { get; protected set; }

    // Private constructor for EF Core
    private PostTagAssignment() : base() { }

    /// <summary>
    /// Creates a new post-tag assignment.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static PostTagAssignment Create(
        Guid postId,
        Guid tagId,
        string? tenantId = null)
    {
        if (postId == Guid.Empty)
            throw new ArgumentException("PostId cannot be empty.", nameof(postId));
        if (tagId == Guid.Empty)
            throw new ArgumentException("TagId cannot be empty.", nameof(tagId));

        return new PostTagAssignment
        {
            Id = Guid.NewGuid(),
            PostId = postId,
            TagId = tagId,
            TenantId = tenantId
        };
    }
}
