
namespace NOIR.Application.Features.Blog.Commands.DeletePost;

/// <summary>
/// Wolverine handler for soft deleting a blog post.
/// </summary>
public class DeletePostCommandHandler
{
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IRepository<PostTag, Guid> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeletePostCommandHandler(
        IRepository<Post, Guid> postRepository,
        IRepository<PostTag, Guid> tagRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _postRepository = postRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<bool>> Handle(
        DeletePostCommand command,
        CancellationToken cancellationToken)
    {
        // Get post with tracking
        var postSpec = new PostByIdForUpdateSpec(command.Id);
        var post = await _postRepository.FirstOrDefaultAsync(postSpec, cancellationToken);

        if (post is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Post with ID '{command.Id}' not found.", "NOIR-BLOG-003"));
        }

        // Batch fetch all tags in single query (fixes N+1)
        if (post.TagAssignments.Any())
        {
            var tagIds = post.TagAssignments.Select(a => a.TagId).ToList();
            var tagsSpec = new TagsByIdsSpec(tagIds);
            var tags = await _tagRepository.ListAsync(tagsSpec, cancellationToken);

            // Decrement tag counts before deleting
            foreach (var tag in tags)
            {
                tag.DecrementPostCount();
            }
        }

        // Clear tag assignments (will be cascaded by EF)
        post.TagAssignments.Clear();

        // Soft delete the post (handled by interceptor)
        _postRepository.Remove(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "BlogPost",
            entityId: post.Id,
            operation: EntityOperation.Deleted,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(true);
    }
}
