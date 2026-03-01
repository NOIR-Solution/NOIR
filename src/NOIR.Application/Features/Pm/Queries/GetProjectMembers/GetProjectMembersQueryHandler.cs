namespace NOIR.Application.Features.Pm.Queries.GetProjectMembers;

public class GetProjectMembersQueryHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetProjectMembersQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<Features.Pm.DTOs.ProjectMemberDto>>> Handle(
        GetProjectMembersQuery query,
        CancellationToken cancellationToken)
    {
        var members = await _dbContext.ProjectMembers
            .Where(m => m.ProjectId == query.ProjectId)
            .Include(m => m.Employee!)
            .OrderBy(m => m.JoinedAt)
            .TagWith("GetProjectMembers")
            .ToListAsync(cancellationToken);

        var items = members.Select(m => new Features.Pm.DTOs.ProjectMemberDto(
            m.Id, m.EmployeeId,
            m.Employee != null ? $"{m.Employee.FirstName} {m.Employee.LastName}" : string.Empty,
            m.Employee?.AvatarUrl,
            m.Role, m.JoinedAt,
            m.Employee?.EmployeeCode, m.Employee?.Position)).ToList();

        return Result.Success(items);
    }
}
