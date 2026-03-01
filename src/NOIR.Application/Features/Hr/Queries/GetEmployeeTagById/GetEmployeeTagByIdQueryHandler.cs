namespace NOIR.Application.Features.Hr.Queries.GetEmployeeTagById;

public class GetEmployeeTagByIdQueryHandler
{
    private readonly IRepository<EmployeeTag, Guid> _tagRepository;

    public GetEmployeeTagByIdQueryHandler(IRepository<EmployeeTag, Guid> tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<Result<EmployeeTagDto>> Handle(
        GetEmployeeTagByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new EmployeeTagByIdSpec(query.Id);
        var tag = await _tagRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (tag is null)
        {
            return Result.Failure<EmployeeTagDto>(
                Error.NotFound($"Employee tag with ID '{query.Id}' not found.", "NOIR-HR-030"));
        }

        return Result.Success(new EmployeeTagDto(
            tag.Id, tag.Name, tag.Category, tag.Color, tag.Description,
            tag.SortOrder, tag.IsActive, tag.TagAssignments.Count,
            tag.CreatedAt, tag.ModifiedAt));
    }
}
