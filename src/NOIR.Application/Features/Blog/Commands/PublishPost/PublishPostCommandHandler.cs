
namespace NOIR.Application.Features.Blog.Commands.PublishPost;

/// <summary>
/// Wolverine handler for publishing or scheduling a blog post.
/// </summary>
public class PublishPostCommandHandler
{
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IRepository<PostCategory, Guid> _categoryRepository;
    private readonly IRepository<PostTag, Guid> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PublishPostCommandHandler(
        IRepository<Post, Guid> postRepository,
        IRepository<PostCategory, Guid> categoryRepository,
        IRepository<PostTag, Guid> tagRepository,
        IUnitOfWork unitOfWork)
    {
        _postRepository = postRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PostDto>> Handle(
        PublishPostCommand command,
        CancellationToken cancellationToken)
    {
        // Get post with tracking
        var postSpec = new PostByIdForUpdateSpec(command.Id);
        var post = await _postRepository.FirstOrDefaultAsync(postSpec, cancellationToken);

        if (post is null)
        {
            return Result.Failure<PostDto>(
                Error.NotFound($"Post with ID '{command.Id}' not found.", "NOIR-BLOG-003"));
        }

        // Publish or schedule
        if (command.ScheduledPublishAt.HasValue)
        {
            if (command.ScheduledPublishAt.Value <= DateTimeOffset.UtcNow)
            {
                return Result.Failure<PostDto>(
                    Error.Validation("ScheduledPublishAt", "Scheduled publish date must be in the future.", "NOIR-BLOG-004"));
            }
            post.Schedule(command.ScheduledPublishAt.Value);
        }
        else
        {
            post.Publish();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get category name and slug for DTO
        string? categoryName = null;
        string? categorySlug = null;
        if (post.CategoryId.HasValue)
        {
            var categorySpec = new CategoryByIdSpec(post.CategoryId.Value);
            var category = await _categoryRepository.FirstOrDefaultAsync(categorySpec, cancellationToken);
            categoryName = category?.Name;
            categorySlug = category?.Slug;
        }

        // Get tags for DTO from post's tag assignments
        var tagDtos = post.TagAssignments
            .Where(ta => ta.Tag != null)
            .Select(ta => new PostTagDto(
                ta.Tag!.Id, ta.Tag.Name, ta.Tag.Slug, ta.Tag.Description,
                ta.Tag.Color, ta.Tag.PostCount, ta.Tag.CreatedAt, ta.Tag.ModifiedAt))
            .ToList();

        return Result.Success(MapToDto(post, categoryName, categorySlug, null, tagDtos));
    }

    private static PostDto MapToDto(
        Post post,
        string? categoryName,
        string? categorySlug,
        string? authorName,
        List<PostTagDto> tags)
    {
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
            categoryName,
            categorySlug,
            post.AuthorId,
            authorName,
            post.ViewCount,
            post.ReadingTimeMinutes,
            tags,
            post.CreatedAt,
            post.ModifiedAt);
    }
}
