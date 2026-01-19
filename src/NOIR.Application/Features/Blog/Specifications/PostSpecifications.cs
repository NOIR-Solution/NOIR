namespace NOIR.Application.Features.Blog.Specifications;

/// <summary>
/// Specification to retrieve posts with optional filtering and pagination.
/// </summary>
public sealed class PostsSpec : Specification<Post>
{
    public PostsSpec(
        string? search = null,
        PostStatus? status = null,
        Guid? categoryId = null,
        Guid? authorId = null,
        int? skip = null,
        int? take = null)
    {
        Query.Where(p => string.IsNullOrEmpty(search) ||
                         p.Title.Contains(search) ||
                         (p.Excerpt != null && p.Excerpt.Contains(search)))
             .Where(p => status == null || p.Status == status)
             .Where(p => categoryId == null || p.CategoryId == categoryId)
             .Where(p => authorId == null || p.AuthorId == authorId)
             .Include(p => p.Category)
             .Include(p => p.FeaturedImage)
             .Include("TagAssignments.Tag")
             .OrderByDescending(p => p.CreatedAt)
             .TagWith("GetPosts");

        if (skip.HasValue)
            Query.Skip(skip.Value);
        if (take.HasValue)
            Query.Take(take.Value);
    }
}

/// <summary>
/// Specification to retrieve published posts for public display.
/// </summary>
public sealed class PublishedPostsSpec : Specification<Post>
{
    public PublishedPostsSpec(
        string? search = null,
        Guid? categoryId = null,
        Guid? tagId = null,
        int? skip = null,
        int? take = null)
    {
        Query.Where(p => p.Status == PostStatus.Published)
             .Where(p => p.PublishedAt != null && p.PublishedAt <= DateTimeOffset.UtcNow)
             .Where(p => string.IsNullOrEmpty(search) ||
                         p.Title.Contains(search) ||
                         (p.Excerpt != null && p.Excerpt.Contains(search)))
             .Where(p => categoryId == null || p.CategoryId == categoryId)
             .Include(p => p.Category)
             .Include(p => p.FeaturedImage)
             .Include("TagAssignments.Tag")
             .OrderByDescending(p => p.PublishedAt)
             .TagWith("GetPublishedPosts");

        if (tagId.HasValue)
        {
            Query.Where(p => p.TagAssignments.Any(ta => ta.TagId == tagId));
        }

        if (skip.HasValue)
            Query.Skip(skip.Value);
        if (take.HasValue)
            Query.Take(take.Value);
    }
}

/// <summary>
/// Specification to find a post by ID with all related data.
/// </summary>
public sealed class PostByIdSpec : Specification<Post>
{
    public PostByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .Include(p => p.Category)
             .Include(p => p.FeaturedImage)
             .Include("TagAssignments.Tag")
             .TagWith("GetPostById");
    }
}

/// <summary>
/// Specification to find a post by ID for update (with tracking).
/// </summary>
public sealed class PostByIdForUpdateSpec : Specification<Post>
{
    public PostByIdForUpdateSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .Include(p => p.TagAssignments)
             .Include(p => p.FeaturedImage)
             .AsTracking()
             .TagWith("GetPostByIdForUpdate");
    }
}

/// <summary>
/// Specification to find a post by slug.
/// </summary>
public sealed class PostBySlugSpec : Specification<Post>
{
    public PostBySlugSpec(string slug, string? tenantId = null)
    {
        Query.Where(p => p.Slug == slug.ToLowerInvariant())
             .Where(p => tenantId == null || p.TenantId == tenantId)
             .Include(p => p.Category)
             .Include(p => p.FeaturedImage)
             .Include("TagAssignments.Tag")
             .TagWith("GetPostBySlug");
    }
}

/// <summary>
/// Specification to find posts scheduled for publication.
/// </summary>
public sealed class ScheduledPostsReadyToPublishSpec : Specification<Post>
{
    public ScheduledPostsReadyToPublishSpec()
    {
        Query.Where(p => p.Status == PostStatus.Scheduled)
             .Where(p => p.ScheduledPublishAt != null && p.ScheduledPublishAt <= DateTimeOffset.UtcNow)
             .AsTracking()
             .TagWith("GetScheduledPostsReadyToPublish");
    }
}

/// <summary>
/// Specification to check if a slug is unique within a tenant.
/// </summary>
public sealed class PostSlugExistsSpec : Specification<Post>
{
    public PostSlugExistsSpec(string slug, string? tenantId = null, Guid? excludeId = null)
    {
        Query.Where(p => p.Slug == slug.ToLowerInvariant())
             .Where(p => tenantId == null || p.TenantId == tenantId)
             .Where(p => excludeId == null || p.Id != excludeId)
             .TagWith("CheckPostSlugExists");
    }
}

/// <summary>
/// Specification to find posts that have a specific tag (for update/deletion).
/// </summary>
public sealed class PostsWithTagForUpdateSpec : Specification<Post>
{
    public PostsWithTagForUpdateSpec(Guid tagId)
    {
        Query.Where(p => p.TagAssignments.Any(ta => ta.TagId == tagId))
             .Include(p => p.TagAssignments)
             .AsTracking()
             .TagWith("GetPostsWithTagForUpdate");
    }
}
