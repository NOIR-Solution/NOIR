
namespace NOIR.Application.Features.Blog.Commands.CreatePost;

/// <summary>
/// Wolverine handler for creating a new blog post.
/// </summary>
public class CreatePostCommandHandler
{
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IRepository<PostTag, Guid> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreatePostCommandHandler(
        IRepository<Post, Guid> postRepository,
        IRepository<PostTag, Guid> tagRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _postRepository = postRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PostDto>> Handle(
        CreatePostCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check if slug already exists
        var slugSpec = new PostSlugExistsSpec(command.Slug, tenantId);
        var existingPost = await _postRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
        if (existingPost != null)
        {
            return Result.Failure<PostDto>(
                Error.Conflict($"A post with slug '{command.Slug}' already exists.", "NOIR-BLOG-001"));
        }

        // Parse author ID from UserId
        if (string.IsNullOrEmpty(command.UserId) || !Guid.TryParse(command.UserId, out var authorId))
        {
            return Result.Failure<PostDto>(
                Error.Validation("UserId", "Invalid author ID.", "NOIR-BLOG-002"));
        }

        // Create the post
        var post = Post.Create(command.Title, command.Slug, authorId, tenantId);

        // Update content
        post.UpdateContent(
            command.Title,
            command.Slug,
            command.Excerpt,
            command.ContentJson,
            command.ContentHtml);

        // Update featured image (prefer MediaFile reference over URL)
        if (command.FeaturedImageId.HasValue)
        {
            post.SetFeaturedImage(command.FeaturedImageId.Value, command.FeaturedImageAlt);
        }
        else if (!string.IsNullOrWhiteSpace(command.FeaturedImageUrl))
        {
            post.UpdateFeaturedImage(command.FeaturedImageUrl, command.FeaturedImageAlt);
        }

        // Update SEO
        post.UpdateSeo(
            command.MetaTitle,
            command.MetaDescription,
            command.CanonicalUrl,
            command.AllowIndexing);

        // Set category
        if (command.CategoryId.HasValue)
        {
            post.SetCategory(command.CategoryId.Value);
        }

        await _postRepository.AddAsync(post, cancellationToken);

        // Handle tags - add to post's collection, EF will save them
        if (command.TagIds?.Any() == true)
        {
            var tagsSpec = new TagsByIdsSpec(command.TagIds);
            var tags = await _tagRepository.ListAsync(tagsSpec, cancellationToken);

            foreach (var tag in tags)
            {
                var assignment = PostTagAssignment.Create(post.Id, tag.Id, tenantId);
                post.TagAssignments.Add(assignment);
                tag.IncrementPostCount();
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return DTO (use command.FeaturedImageUrl since FeaturedImage isn't loaded)
        return Result.Success(MapToDto(post, command.FeaturedImageUrl, null, null, null, []));
    }

    private static PostDto MapToDto(
        Post post,
        string? featuredImageUrl,
        string? categoryName,
        string? categorySlug,
        string? authorName,
        List<PostTagDto> tags)
    {
        return new PostDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Excerpt,
            post.ContentJson,
            post.ContentHtml,
            post.FeaturedImageId,
            featuredImageUrl ?? post.FeaturedImageUrl,
            post.FeaturedImageAlt,
            null, // FeaturedImageWidth - not loaded for new posts
            null, // FeaturedImageHeight - not loaded for new posts
            null, // FeaturedImageThumbHash - not loaded for new posts
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
