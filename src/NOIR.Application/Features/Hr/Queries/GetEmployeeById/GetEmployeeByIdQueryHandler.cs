namespace NOIR.Application.Features.Hr.Queries.GetEmployeeById;

public class GetEmployeeByIdQueryHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IApplicationDbContext _dbContext;

    public GetEmployeeByIdQueryHandler(
        IRepository<Employee, Guid> employeeRepository,
        IApplicationDbContext dbContext)
    {
        _employeeRepository = employeeRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<Features.Hr.DTOs.EmployeeDto>> Handle(
        GetEmployeeByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.EmployeeByIdReadOnlySpec(query.Id);
        var employee = await _employeeRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                Error.NotFound($"Employee with ID '{query.Id}' not found.", "NOIR-HR-010"));
        }

        var directReports = employee.DirectReports
            .Select(dr => new Features.Hr.DTOs.DirectReportDto(
                dr.Id, dr.EmployeeCode, $"{dr.FirstName} {dr.LastName}",
                dr.AvatarUrl, dr.Position, dr.Status))
            .ToList();

        // Fetch tag assignments
        var tags = await _dbContext.EmployeeTagAssignments
            .Where(a => a.EmployeeId == query.Id)
            .Include(a => a.EmployeeTag!)
            .Where(a => a.EmployeeTag != null)
            .Select(a => new Features.Hr.DTOs.TagBriefDto(
                a.EmployeeTag!.Id, a.EmployeeTag.Name, a.EmployeeTag.Category, a.EmployeeTag.Color))
            .ToListAsync(cancellationToken);

        return Result.Success(new Features.Hr.DTOs.EmployeeDto(
            employee.Id, employee.EmployeeCode, employee.FirstName, employee.LastName,
            employee.Email, employee.Phone, employee.AvatarUrl,
            employee.DepartmentId, employee.Department?.Name ?? "", employee.Position,
            employee.ManagerId,
            employee.Manager != null ? $"{employee.Manager.FirstName} {employee.Manager.LastName}" : null,
            employee.UserId, employee.UserId != null,
            employee.JoinDate, employee.EndDate, employee.Status, employee.EmploymentType,
            employee.Notes, directReports, tags, employee.CreatedAt, employee.ModifiedAt));
    }
}
