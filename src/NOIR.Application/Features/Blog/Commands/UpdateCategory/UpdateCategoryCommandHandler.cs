
namespace NOIR.Application.Features.Blog.Commands.UpdateCategory;

/// <summary>
/// Wolverine handler for updating an existing blog category.
/// </summary>
public class UpdateCategoryCommandHandler
{
    private readonly IRepository<PostCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateCategoryCommandHandler(
        IRepository<PostCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PostCategoryDto>> Handle(
        UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Get category with tracking
        var categorySpec = new CategoryByIdForUpdateSpec(command.Id);
        var category = await _categoryRepository.FirstOrDefaultAsync(categorySpec, cancellationToken);

        if (category is null)
        {
            return Result.Failure<PostCategoryDto>(
                Error.NotFound($"Category with ID '{command.Id}' not found.", "NOIR-BLOG-007"));
        }

        // Check if slug changed and is unique
        if (category.Slug != command.Slug.ToLowerInvariant())
        {
            var slugSpec = new CategorySlugExistsSpec(command.Slug, tenantId, command.Id);
            var existingCategory = await _categoryRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
            if (existingCategory != null)
            {
                return Result.Failure<PostCategoryDto>(
                    Error.Conflict($"A category with slug '{command.Slug}' already exists.", "NOIR-BLOG-005"));
            }
        }

        // Validate parent exists if specified
        string? parentName = null;
        if (command.ParentId.HasValue)
        {
            // Prevent circular reference
            if (command.ParentId == command.Id)
            {
                return Result.Failure<PostCategoryDto>(
                    Error.Validation("ParentId", "A category cannot be its own parent.", "NOIR-BLOG-008"));
            }

            var parentSpec = new CategoryByIdSpec(command.ParentId.Value);
            var parent = await _categoryRepository.FirstOrDefaultAsync(parentSpec, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<PostCategoryDto>(
                    Error.NotFound($"Parent category with ID '{command.ParentId}' not found.", "NOIR-BLOG-006"));
            }
            parentName = parent.Name;
        }

        // Update category
        category.Update(
            command.Name,
            command.Slug,
            command.Description,
            command.ParentId);

        category.UpdateSeo(command.MetaTitle, command.MetaDescription);
        category.UpdateImage(command.ImageUrl);
        category.SetSortOrder(command.SortOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(category, parentName));
    }

    private static PostCategoryDto MapToDto(PostCategory category, string? parentName)
    {
        return new PostCategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.MetaTitle,
            category.MetaDescription,
            category.ImageUrl,
            category.SortOrder,
            category.PostCount,
            category.ParentId,
            parentName,
            null, // Children not loaded
            category.CreatedAt,
            category.ModifiedAt);
    }
}
