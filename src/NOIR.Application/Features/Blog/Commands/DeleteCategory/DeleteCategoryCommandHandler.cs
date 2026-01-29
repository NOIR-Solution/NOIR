
namespace NOIR.Application.Features.Blog.Commands.DeleteCategory;

/// <summary>
/// Wolverine handler for soft deleting a blog category.
/// </summary>
public class DeleteCategoryCommandHandler
{
    private readonly IRepository<PostCategory, Guid> _categoryRepository;
    private readonly IRepository<Post, Guid> _postRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCategoryCommandHandler(
        IRepository<PostCategory, Guid> categoryRepository,
        IRepository<Post, Guid> postRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _postRepository = postRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        DeleteCategoryCommand command,
        CancellationToken cancellationToken)
    {
        // Get category with tracking
        var categorySpec = new CategoryByIdForUpdateSpec(command.Id);
        var category = await _categoryRepository.FirstOrDefaultAsync(categorySpec, cancellationToken);

        if (category is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Category with ID '{command.Id}' not found.", "NOIR-BLOG-007"));
        }

        // Check for child categories (efficient EXISTS query)
        var childrenSpec = new CategoryHasChildrenSpec(command.Id);
        var hasChildren = await _categoryRepository.AnyAsync(childrenSpec, cancellationToken);
        if (hasChildren)
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete a category that has child categories. Please delete or reassign child categories first.", "NOIR-BLOG-009"));
        }

        // Check for posts in this category (efficient EXISTS query)
        var postsSpec = new CategoryHasPostsSpec(command.Id);
        var hasPosts = await _postRepository.AnyAsync(postsSpec, cancellationToken);
        if (hasPosts)
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete a category that has posts. Please reassign posts to another category first.", "NOIR-BLOG-010"));
        }

        // Soft delete the category (handled by interceptor)
        _categoryRepository.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
