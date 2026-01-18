using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.Features.Blog.Commands.UpdatePost;

/// <summary>
/// Wolverine handler for updating an existing blog post.
/// </summary>
public class UpdatePostCommandHandler
{
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IRepository<PostTag, Guid> _tagRepository;
    private readonly IRepository<PostCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdatePostCommandHandler(
        IRepository<Post, Guid> postRepository,
        IRepository<PostTag, Guid> tagRepository,
        IRepository<PostCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _postRepository = postRepository;
        _tagRepository = tagRepository;
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PostDto>> Handle(
        UpdatePostCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Get post with tracking
        var postSpec = new PostByIdForUpdateSpec(command.Id);
        var post = await _postRepository.FirstOrDefaultAsync(postSpec, cancellationToken);

        if (post is null)
        {
            return Result.Failure<PostDto>(
                Error.NotFound($"Post with ID '{command.Id}' not found.", "NOIR-BLOG-003"));
        }

        // Check if slug changed and is unique
        if (post.Slug != command.Slug.ToLowerInvariant())
        {
            var slugSpec = new PostSlugExistsSpec(command.Slug, tenantId, command.Id);
            var existingPost = await _postRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
            if (existingPost != null)
            {
                return Result.Failure<PostDto>(
                    Error.Conflict($"A post with slug '{command.Slug}' already exists.", "NOIR-BLOG-001"));
            }
        }

        // Update content
        post.UpdateContent(
            command.Title,
            command.Slug,
            command.Excerpt,
            command.ContentJson,
            command.ContentHtml);

        // Update featured image
        post.UpdateFeaturedImage(command.FeaturedImageUrl, command.FeaturedImageAlt);

        // Update SEO
        post.UpdateSeo(
            command.MetaTitle,
            command.MetaDescription,
            command.CanonicalUrl,
            command.AllowIndexing);

        // Set category
        post.SetCategory(command.CategoryId);

        // Handle tag changes
        await UpdateTagAssignmentsAsync(post, command.TagIds ?? [], tenantId, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Get category name for DTO
        string? categoryName = null;
        if (post.CategoryId.HasValue)
        {
            var categorySpec = new CategoryByIdSpec(post.CategoryId.Value);
            var category = await _categoryRepository.FirstOrDefaultAsync(categorySpec, cancellationToken);
            categoryName = category?.Name;
        }

        // Get tags for DTO
        var tagDtos = post.TagAssignments
            .Where(ta => ta.Tag != null)
            .Select(ta => new PostTagDto(
                ta.Tag!.Id, ta.Tag.Name, ta.Tag.Slug, ta.Tag.Description,
                ta.Tag.Color, ta.Tag.PostCount, ta.Tag.CreatedAt, ta.Tag.ModifiedAt))
            .ToList();

        return Result.Success(MapToDto(post, categoryName, null, tagDtos));
    }

    private async Task UpdateTagAssignmentsAsync(
        Post post,
        List<Guid> newTagIds,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        // Get existing tag IDs
        var existingIds = post.TagAssignments.Select(ta => ta.TagId).ToHashSet();
        var newIds = newTagIds.ToHashSet();

        // Find tags to remove
        var toRemove = post.TagAssignments.Where(ta => !newIds.Contains(ta.TagId)).ToList();
        foreach (var assignment in toRemove)
        {
            post.TagAssignments.Remove(assignment);

            // Decrement tag count
            var tagSpec = new TagByIdForUpdateSpec(assignment.TagId);
            var tag = await _tagRepository.FirstOrDefaultAsync(tagSpec, cancellationToken);
            tag?.DecrementPostCount();
        }

        // Find tags to add
        var toAdd = newIds.Except(existingIds).ToList();
        if (toAdd.Any())
        {
            var tagsSpec = new TagsByIdsSpec(toAdd);
            var tags = await _tagRepository.ListAsync(tagsSpec, cancellationToken);

            foreach (var tag in tags)
            {
                var assignment = PostTagAssignment.Create(post.Id, tag.Id, tenantId);
                post.TagAssignments.Add(assignment);
                tag.IncrementPostCount();
            }
        }
    }

    private static PostDto MapToDto(
        Post post,
        string? categoryName,
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
            post.FeaturedImageUrl,
            post.FeaturedImageAlt,
            post.Status,
            post.PublishedAt,
            post.ScheduledPublishAt,
            post.MetaTitle,
            post.MetaDescription,
            post.CanonicalUrl,
            post.AllowIndexing,
            post.CategoryId,
            categoryName,
            post.AuthorId,
            authorName,
            post.ViewCount,
            post.ReadingTimeMinutes,
            tags,
            post.CreatedAt,
            post.ModifiedAt);
    }
}
