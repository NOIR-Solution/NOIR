using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.Features.Blog.Queries.GetPost;

/// <summary>
/// Wolverine handler for getting a single blog post.
/// </summary>
public class GetPostQueryHandler
{
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly ICurrentUser _currentUser;

    public GetPostQueryHandler(
        IRepository<Post, Guid> postRepository,
        ICurrentUser currentUser)
    {
        _postRepository = postRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PostDto>> Handle(
        GetPostQuery query,
        CancellationToken cancellationToken)
    {
        Post? post = null;

        if (query.Id.HasValue)
        {
            var spec = new PostByIdSpec(query.Id.Value);
            post = await _postRepository.FirstOrDefaultAsync(spec, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(query.Slug))
        {
            var tenantId = _currentUser.TenantId;
            var spec = new PostBySlugSpec(query.Slug, tenantId);
            post = await _postRepository.FirstOrDefaultAsync(spec, cancellationToken);
        }
        else
        {
            return Result.Failure<PostDto>(
                Error.Validation("Id", "Either ID or Slug must be provided.", "NOIR-BLOG-013"));
        }

        if (post is null)
        {
            return Result.Failure<PostDto>(
                Error.NotFound("Post not found.", "NOIR-BLOG-003"));
        }

        return Result.Success(MapToDto(post));
    }

    private static PostDto MapToDto(Post post)
    {
        var tags = post.TagAssignments
            .Where(ta => ta.Tag != null)
            .Select(ta => new PostTagDto(
                ta.Tag!.Id,
                ta.Tag.Name,
                ta.Tag.Slug,
                ta.Tag.Description,
                ta.Tag.Color,
                ta.Tag.PostCount,
                ta.Tag.CreatedAt,
                ta.Tag.ModifiedAt))
            .ToList();

        // Resolve featured image URL: prefer MediaFile.DefaultUrl, fallback to direct URL
        var featuredImageUrl = post.FeaturedImage?.DefaultUrl ?? post.FeaturedImageUrl;

        return new PostDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Excerpt,
            post.ContentJson,
            post.ContentHtml,
            post.FeaturedImageId,
            featuredImageUrl,
            post.FeaturedImageAlt,
            post.FeaturedImage?.Width,
            post.FeaturedImage?.Height,
            post.FeaturedImage?.ThumbHash,
            post.Status,
            post.PublishedAt,
            post.ScheduledPublishAt,
            post.MetaTitle,
            post.MetaDescription,
            post.CanonicalUrl,
            post.AllowIndexing,
            post.CategoryId,
            post.Category?.Name,
            post.Category?.Slug,
            post.AuthorId,
            null, // AuthorName would require user lookup
            post.ViewCount,
            post.ReadingTimeMinutes,
            tags,
            post.CreatedAt,
            post.ModifiedAt);
    }
}
