namespace NOIR.Application.Features.Hr.Commands.UpdateEmployee;

public class UpdateEmployeeCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IEmployeeHierarchyService _hierarchyService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    private const int MaxHierarchyDepth = 20;

    public UpdateEmployeeCommandHandler(
        IRepository<Employee, Guid> employeeRepository,
        IRepository<Department, Guid> departmentRepository,
        IEmployeeHierarchyService hierarchyService,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _hierarchyService = hierarchyService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Hr.DTOs.EmployeeDto>> Handle(
        UpdateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var spec = new Specifications.EmployeeByIdSpec(command.Id);
        var employee = await _employeeRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                Error.NotFound($"Employee with ID '{command.Id}' not found.", "NOIR-HR-010"));
        }

        // Validate email uniqueness (excluding self)
        if (employee.Email != command.Email.Trim().ToLowerInvariant())
        {
            var emailSpec = new Specifications.EmployeeByEmailSpec(command.Email, tenantId, command.Id);
            var existingByEmail = await _employeeRepository.FirstOrDefaultAsync(emailSpec, cancellationToken);
            if (existingByEmail is not null)
            {
                return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                    Error.Conflict($"An employee with email '{command.Email}' already exists.", "NOIR-HR-001"));
            }
        }

        // Validate department exists
        var deptSpec = new Specifications.DepartmentByIdSpec(command.DepartmentId);
        var department = await _departmentRepository.FirstOrDefaultAsync(deptSpec, cancellationToken);
        if (department is null)
        {
            return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                Error.NotFound($"Department with ID '{command.DepartmentId}' not found.", "NOIR-HR-002"));
        }

        // Validate manager hierarchy if manager changes
        if (command.ManagerId.HasValue)
        {
            // Self-reference guard
            if (command.ManagerId.Value == command.Id)
            {
                return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                    Error.Validation("ManagerId", "An employee cannot be their own manager."));
            }

            // Validate manager exists
            var managerSpec = new Specifications.EmployeeByIdSpec(command.ManagerId.Value);
            var manager = await _employeeRepository.FirstOrDefaultAsync(managerSpec, cancellationToken);
            if (manager is null)
            {
                return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                    Error.NotFound($"Manager with ID '{command.ManagerId}' not found.", "NOIR-HR-003"));
            }

            // Circular reference and depth check
            var chain = await _hierarchyService.GetAncestorChainAsync(
                command.ManagerId.Value, MaxHierarchyDepth, tenantId, cancellationToken);

            if (chain.AncestorIds.Contains(command.Id))
            {
                return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                    Error.Validation("ManagerId", "Setting this manager would create a circular reporting chain."));
            }

            if (chain.Depth + 1 > MaxHierarchyDepth)
            {
                return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                    Error.Validation("ManagerId", $"Manager hierarchy cannot exceed {MaxHierarchyDepth} levels."));
            }
        }

        // Apply updates
        employee.UpdateBasicInfo(
            command.FirstName, command.LastName, command.Email,
            command.Phone, command.AvatarUrl, command.Position,
            command.EmploymentType, command.Notes);

        employee.UpdateDepartment(command.DepartmentId);
        employee.UpdateManager(command.ManagerId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Employee",
            entityId: employee.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(MapToDto(employee, department.Name));
    }

    private static Features.Hr.DTOs.EmployeeDto MapToDto(Employee e, string departmentName) =>
        new(
            e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.Email, e.Phone, e.AvatarUrl,
            e.DepartmentId, departmentName, e.Position,
            e.ManagerId, e.Manager != null ? $"{e.Manager.FirstName} {e.Manager.LastName}" : null,
            e.UserId, e.UserId != null, e.JoinDate, e.EndDate, e.Status, e.EmploymentType, e.Notes,
            [], [], e.CreatedAt, e.ModifiedAt);
}
