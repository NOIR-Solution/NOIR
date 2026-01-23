
namespace NOIR.Application.Features.Blog.Queries.GetTags;

/// <summary>
/// Wolverine handler for getting a list of blog tags.
/// </summary>
public class GetTagsQueryHandler
{
    private readonly IRepository<PostTag, Guid> _tagRepository;

    public GetTagsQueryHandler(IRepository<PostTag, Guid> tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<Result<List<PostTagListDto>>> Handle(
        GetTagsQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new TagsSpec(query.Search);
        var tags = await _tagRepository.ListAsync(spec, cancellationToken);

        var result = tags.Select(t => new PostTagListDto(
            t.Id,
            t.Name,
            t.Slug,
            t.Description,
            t.Color,
            t.PostCount
        )).ToList();

        return Result.Success(result);
    }
}
