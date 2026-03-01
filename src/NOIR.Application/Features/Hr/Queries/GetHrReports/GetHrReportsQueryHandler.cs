namespace NOIR.Application.Features.Hr.Queries.GetHrReports;

/// <summary>
/// Wolverine handler for HR reports aggregate queries.
/// Uses IApplicationDbContext directly for efficient GROUP BY operations.
/// </summary>
public class GetHrReportsQueryHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetHrReportsQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<HrReportsDto>> Handle(
        GetHrReportsQuery query,
        CancellationToken cancellationToken)
    {
        // Run all aggregate queries sequentially — DbContext is not thread-safe
        var headcountByDept = await _dbContext.Employees
            .Where(e => !e.IsDeleted && e.Status == EmployeeStatus.Active)
            .GroupBy(e => new { e.DepartmentId, e.Department!.Name })
            .Select(g => new DepartmentHeadcountDto(g.Key.DepartmentId, g.Key.Name, g.Count()))
            .OrderByDescending(x => x.Count)
            .TagWith("GetHrReports_HeadcountByDepartment")
            .ToListAsync(cancellationToken);

        var tagDistribution = await _dbContext.EmployeeTagAssignments
            .Where(a => !a.IsDeleted && a.EmployeeTag != null && a.EmployeeTag.IsActive)
            .GroupBy(a => new { a.EmployeeTagId, a.EmployeeTag!.Name, a.EmployeeTag.Category, a.EmployeeTag.Color })
            .Select(g => new TagDistributionDto(g.Key.EmployeeTagId, g.Key.Name, g.Key.Category, g.Key.Color, g.Count()))
            .OrderByDescending(x => x.Count)
            .TagWith("GetHrReports_TagDistribution")
            .ToListAsync(cancellationToken);

        var employmentTypeBreakdown = await _dbContext.Employees
            .Where(e => !e.IsDeleted && e.Status == EmployeeStatus.Active)
            .GroupBy(e => e.EmploymentType)
            .Select(g => new EmploymentTypeBreakdownDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .TagWith("GetHrReports_EmploymentTypeBreakdown")
            .ToListAsync(cancellationToken);

        var statusBreakdown = await _dbContext.Employees
            .Where(e => !e.IsDeleted)
            .GroupBy(e => e.Status)
            .Select(g => new StatusBreakdownDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .TagWith("GetHrReports_StatusBreakdown")
            .ToListAsync(cancellationToken);

        var totalActiveEmployees = headcountByDept.Sum(x => x.Count);

        var totalDepartments = await _dbContext.Departments
            .Where(d => !d.IsDeleted && d.IsActive)
            .TagWith("GetHrReports_TotalDepartments")
            .CountAsync(cancellationToken);

        return Result.Success(new HrReportsDto(
            headcountByDept,
            tagDistribution,
            employmentTypeBreakdown,
            statusBreakdown,
            totalActiveEmployees,
            totalDepartments));
    }
}
