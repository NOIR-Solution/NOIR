using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.Features.Blog.Commands.CreateCategory;

/// <summary>
/// Wolverine handler for creating a new blog category.
/// </summary>
public class CreateCategoryCommandHandler
{
    private readonly IRepository<PostCategory, Guid> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateCategoryCommandHandler(
        IRepository<PostCategory, Guid> categoryRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PostCategoryDto>> Handle(
        CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check if slug already exists
        var slugSpec = new CategorySlugExistsSpec(command.Slug, tenantId);
        var existingCategory = await _categoryRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
        if (existingCategory != null)
        {
            return Result.Failure<PostCategoryDto>(
                Error.Conflict($"A category with slug '{command.Slug}' already exists.", "NOIR-BLOG-005"));
        }

        // Validate parent exists if specified
        string? parentName = null;
        if (command.ParentId.HasValue)
        {
            var parentSpec = new CategoryByIdSpec(command.ParentId.Value);
            var parent = await _categoryRepository.FirstOrDefaultAsync(parentSpec, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<PostCategoryDto>(
                    Error.NotFound($"Parent category with ID '{command.ParentId}' not found.", "NOIR-BLOG-006"));
            }
            parentName = parent.Name;
        }

        // Create the category
        var category = PostCategory.Create(
            command.Name,
            command.Slug,
            command.Description,
            command.ParentId,
            tenantId);

        // Set sort order
        category.SetSortOrder(command.SortOrder);

        // Update SEO fields
        category.UpdateSeo(command.MetaTitle, command.MetaDescription);

        // Update image
        if (!string.IsNullOrWhiteSpace(command.ImageUrl))
        {
            category.UpdateImage(command.ImageUrl);
        }

        await _categoryRepository.AddAsync(category, cancellationToken);
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
