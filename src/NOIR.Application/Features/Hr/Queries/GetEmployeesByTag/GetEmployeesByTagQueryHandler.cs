namespace NOIR.Application.Features.Hr.Queries.GetEmployeesByTag;

public class GetEmployeesByTagQueryHandler
{
    private readonly IRepository<EmployeeTag, Guid> _tagRepository;
    private readonly IApplicationDbContext _dbContext;

    public GetEmployeesByTagQueryHandler(
        IRepository<EmployeeTag, Guid> tagRepository,
        IApplicationDbContext dbContext)
    {
        _tagRepository = tagRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<PagedResult<EmployeeListDto>>> Handle(
        GetEmployeesByTagQuery query,
        CancellationToken cancellationToken)
    {
        // Validate tag exists
        var tagExists = await _tagRepository.ExistsAsync(query.TagId, cancellationToken);
        if (!tagExists)
        {
            return Result.Failure<PagedResult<EmployeeListDto>>(
                Error.NotFound($"Employee tag with ID '{query.TagId}' not found.", "NOIR-HR-037"));
        }

        var skip = (query.Page - 1) * query.PageSize;

        // Count total
        var totalCount = await _dbContext.EmployeeTagAssignments
            .Where(a => a.EmployeeTagId == query.TagId)
            .CountAsync(cancellationToken);

        // Get paginated assignments with employee details
        var assignments = await _dbContext.EmployeeTagAssignments
            .Where(a => a.EmployeeTagId == query.TagId)
            .Include(a => a.Employee!)
                .ThenInclude(e => e.Department!)
            .Include(a => a.Employee!)
                .ThenInclude(e => e.Manager!)
            .OrderByDescending(a => a.AssignedAt)
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = assignments
            .Where(a => a.Employee is not null)
            .Select(a => new EmployeeListDto(
                a.Employee!.Id, a.Employee.EmployeeCode,
                a.Employee.FirstName, a.Employee.LastName,
                a.Employee.Email, a.Employee.AvatarUrl,
                a.Employee.Department?.Name ?? "",
                a.Employee.Position,
                a.Employee.Manager != null ? $"{a.Employee.Manager.FirstName} {a.Employee.Manager.LastName}" : null,
                a.Employee.Status, a.Employee.EmploymentType))
            .ToList();

        return Result.Success(PagedResult<EmployeeListDto>.Create(
            items, totalCount, query.Page - 1, query.PageSize));
    }
}
