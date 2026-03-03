namespace NOIR.Application.Features.Hr.Commands.DeactivateEmployee;

public class DeactivateEmployeeCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeactivateEmployeeCommandHandler(
        IRepository<Employee, Guid> employeeRepository,
        IRepository<Department, Guid> departmentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Hr.DTOs.EmployeeDto>> Handle(
        DeactivateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.EmployeeByIdSpec(command.Id);
        var employee = await _employeeRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                Error.NotFound($"Employee with ID '{command.Id}' not found.", "NOIR-HR-010"));
        }

        // Deactivate the employee
        employee.Deactivate(command.Status);

        // Cascade: null ManagerId on direct reports
        var directReportsSpec = new Specifications.EmployeesByManagerIdSpec(command.Id);
        var directReports = await _employeeRepository.ListAsync(directReportsSpec, cancellationToken);
        foreach (var report in directReports)
        {
            report.UpdateManager(null);
        }

        // Cascade: null Department.ManagerId where this employee is dept manager
        var deptManagerSpec = new Specifications.DepartmentsByManagerIdSpec(command.Id);
        var managedDepts = await _departmentRepository.ListAsync(deptManagerSpec, cancellationToken);
        foreach (var dept in managedDepts)
        {
            dept.Update(dept.Name, dept.Code, dept.Description, null, dept.ParentDepartmentId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Employee",
            entityId: employee.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        var departmentName = employee.Department?.Name ?? "";
        return Result.Success(new Features.Hr.DTOs.EmployeeDto(
            employee.Id, employee.EmployeeCode, employee.FirstName, employee.LastName,
            employee.Email, employee.Phone, employee.AvatarUrl,
            employee.DepartmentId, departmentName, employee.Position,
            employee.ManagerId, null,
            employee.UserId, employee.UserId != null,
            employee.JoinDate, employee.EndDate, employee.Status, employee.EmploymentType,
            employee.Notes, [], [], employee.CreatedAt, employee.ModifiedAt));
    }
}
