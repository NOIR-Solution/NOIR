
namespace NOIR.Application.Features.Blog.Commands.DeletePost;

/// <summary>
/// Wolverine handler for soft deleting a blog post.
/// </summary>
public class DeletePostCommandHandler
{
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IRepository<PostTag, Guid> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeletePostCommandHandler(
        IRepository<Post, Guid> postRepository,
        IRepository<PostTag, Guid> tagRepository,
        IUnitOfWork unitOfWork)
    {
        _postRepository = postRepository;
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
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

        // Decrement tag counts before deleting
        foreach (var assignment in post.TagAssignments)
        {
            var tagSpec = new TagByIdForUpdateSpec(assignment.TagId);
            var tag = await _tagRepository.FirstOrDefaultAsync(tagSpec, cancellationToken);
            tag?.DecrementPostCount();
        }

        // Clear tag assignments (will be cascaded by EF)
        post.TagAssignments.Clear();

        // Soft delete the post (handled by interceptor)
        _postRepository.Remove(post);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
