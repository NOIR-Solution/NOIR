namespace NOIR.Application.Features.Hr.Queries.GetTags;

public class GetTagsQueryHandler
{
    private readonly IRepository<EmployeeTag, Guid> _tagRepository;

    public GetTagsQueryHandler(IRepository<EmployeeTag, Guid> tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<Result<List<EmployeeTagDto>>> Handle(
        GetTagsQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new AllEmployeeTagsSpec(query.Category, query.IsActive);
        var tags = await _tagRepository.ListAsync(spec, cancellationToken);

        var items = tags.Select(t => new EmployeeTagDto(
            t.Id, t.Name, t.Category, t.Color, t.Description,
            t.SortOrder, t.IsActive, t.EmployeeCount,
            t.CreatedAt, t.ModifiedAt)).ToList();

        return Result.Success(items);
    }
}
