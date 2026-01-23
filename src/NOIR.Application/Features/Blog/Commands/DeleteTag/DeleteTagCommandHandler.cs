
namespace NOIR.Application.Features.Blog.Commands.DeleteTag;

/// <summary>
/// Wolverine handler for soft deleting a blog tag.
/// </summary>
public class DeleteTagCommandHandler
{
    private readonly IRepository<PostTag, Guid> _tagRepository;
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTagCommandHandler(
        IRepository<PostTag, Guid> tagRepository,
        IRepository<Post, Guid> postRepository,
        IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _postRepository = postRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        DeleteTagCommand command,
        CancellationToken cancellationToken)
    {
        // Get tag with tracking
        var tagSpec = new TagByIdForUpdateSpec(command.Id);
        var tag = await _tagRepository.FirstOrDefaultAsync(tagSpec, cancellationToken);

        if (tag is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Tag with ID '{command.Id}' not found.", "NOIR-BLOG-012"));
        }

        // Find all posts that have this tag and remove the assignment
        var postsWithTagSpec = new PostsWithTagForUpdateSpec(command.Id);
        var postsWithTag = await _postRepository.ListAsync(postsWithTagSpec, cancellationToken);

        foreach (var post in postsWithTag)
        {
            var assignment = post.TagAssignments.FirstOrDefault(ta => ta.TagId == command.Id);
            if (assignment != null)
            {
                post.TagAssignments.Remove(assignment);
            }
        }

        // Soft delete the tag (handled by interceptor)
        _tagRepository.Remove(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
