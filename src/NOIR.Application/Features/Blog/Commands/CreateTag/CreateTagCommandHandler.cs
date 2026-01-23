
namespace NOIR.Application.Features.Blog.Commands.CreateTag;

/// <summary>
/// Wolverine handler for creating a new blog tag.
/// </summary>
public class CreateTagCommandHandler
{
    private readonly IRepository<PostTag, Guid> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateTagCommandHandler(
        IRepository<PostTag, Guid> tagRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<PostTagDto>> Handle(
        CreateTagCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check if slug already exists
        var slugSpec = new TagSlugExistsSpec(command.Slug, tenantId);
        var existingTag = await _tagRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
        if (existingTag != null)
        {
            return Result.Failure<PostTagDto>(
                Error.Conflict($"A tag with slug '{command.Slug}' already exists.", "NOIR-BLOG-011"));
        }

        // Create the tag
        var tag = PostTag.Create(
            command.Name,
            command.Slug,
            command.Description,
            command.Color,
            tenantId);

        await _tagRepository.AddAsync(tag, cancellationToken);
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
