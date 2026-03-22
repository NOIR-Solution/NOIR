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
        var headcountByDeptRaw = await _dbContext.Employees
            .Where(e => !e.IsDeleted && e.Status == EmployeeStatus.Active)
            .GroupBy(e => new { e.DepartmentId, DepartmentName = e.Department!.Name })
            .Select(g => new { g.Key.DepartmentId, g.Key.DepartmentName, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .TagWith("GetHrReports_HeadcountByDepartment")
            .ToListAsync(cancellationToken);

        var headcountByDept = headcountByDeptRaw
            .Select(x => new DepartmentHeadcountDto(x.DepartmentId, x.DepartmentName ?? "Unknown", x.Count))
            .ToList();

        var tagDistributionRaw = await _dbContext.EmployeeTagAssignments
            .Where(a => !a.IsDeleted && a.EmployeeTag != null)
            .GroupBy(a => new { a.EmployeeTagId, TagName = a.EmployeeTag!.Name, a.EmployeeTag.Category, a.EmployeeTag.Color })
            .Select(g => new { g.Key.EmployeeTagId, g.Key.TagName, g.Key.Category, g.Key.Color, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .TagWith("GetHrReports_TagDistribution")
            .ToListAsync(cancellationToken);

        var tagDistribution = tagDistributionRaw
            .Select(x => new TagDistributionDto(x.EmployeeTagId, x.TagName, x.Category, x.Color ?? "#6366f1", x.Count))
            .ToList();

        var employmentTypeRaw = await _dbContext.Employees
            .Where(e => !e.IsDeleted && e.Status == EmployeeStatus.Active)
            .GroupBy(e => e.EmploymentType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .TagWith("GetHrReports_EmploymentTypeBreakdown")
            .ToListAsync(cancellationToken);

        var employmentTypeBreakdown = employmentTypeRaw
            .Select(x => new EmploymentTypeBreakdownDto(x.Type, x.Count))
            .ToList();

        var statusRaw = await _dbContext.Employees
            .Where(e => !e.IsDeleted)
            .GroupBy(e => e.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .TagWith("GetHrReports_StatusBreakdown")
            .ToListAsync(cancellationToken);

        var statusBreakdown = statusRaw
            .Select(x => new StatusBreakdownDto(x.Status, x.Count))
            .ToList();

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
