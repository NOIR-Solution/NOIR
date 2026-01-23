
namespace NOIR.Application.Features.Blog.Queries.GetTagById;

/// <summary>
/// Wolverine handler for getting a single blog tag by ID.
/// </summary>
public class GetTagByIdQueryHandler
{
    private readonly IRepository<PostTag, Guid> _tagRepository;

    public GetTagByIdQueryHandler(IRepository<PostTag, Guid> tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<Result<PostTagDto>> Handle(
        GetTagByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new TagByIdSpec(query.Id);
        var tag = await _tagRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (tag is null)
        {
            return Result.Failure<PostTagDto>(
                Error.NotFound("Tag not found.", "NOIR-BLOG-021"));
        }

        return Result.Success(new PostTagDto(
            tag.Id,
            tag.Name,
            tag.Slug,
            tag.Description,
            tag.Color,
            tag.PostCount,
            tag.CreatedAt,
            tag.ModifiedAt));
    }
}
