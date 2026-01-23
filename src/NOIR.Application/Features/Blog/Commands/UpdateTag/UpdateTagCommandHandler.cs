
namespace NOIR.Application.Features.Blog.Commands.UpdateTag;

/// <summary>
/// Wolverine handler for updating an existing blog tag.
/// </summary>
public class UpdateTagCommandHandler
{
    private readonly IRepository<PostTag, Guid> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateTagCommandHandler(
        IRepository<PostTag, Guid> tagRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PostTagDto>> Handle(
        UpdateTagCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Get tag with tracking
        var tagSpec = new TagByIdForUpdateSpec(command.Id);
        var tag = await _tagRepository.FirstOrDefaultAsync(tagSpec, cancellationToken);

        if (tag is null)
        {
            return Result.Failure<PostTagDto>(
                Error.NotFound($"Tag with ID '{command.Id}' not found.", "NOIR-BLOG-012"));
        }

        // Check if slug changed and is unique
        if (tag.Slug != command.Slug.ToLowerInvariant())
        {
            var slugSpec = new TagSlugExistsSpec(command.Slug, tenantId, command.Id);
            var existingTag = await _tagRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
            if (existingTag != null)
            {
                return Result.Failure<PostTagDto>(
                    Error.Conflict($"A tag with slug '{command.Slug}' already exists.", "NOIR-BLOG-011"));
            }
        }

        // Update tag
        tag.Update(
            command.Name,
            command.Slug,
            command.Description,
            command.Color);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(MapToDto(tag));
    }

    private static PostTagDto MapToDto(PostTag tag)
    {
        return new PostTagDto(
            tag.Id,
            tag.Name,
            tag.Slug,
            tag.Description,
            tag.Color,
            tag.PostCount,
            tag.CreatedAt,
            tag.ModifiedAt);
    }
}
