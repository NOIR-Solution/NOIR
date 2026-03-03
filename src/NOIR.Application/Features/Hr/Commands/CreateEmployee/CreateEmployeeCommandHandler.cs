namespace NOIR.Application.Features.Hr.Commands.CreateEmployee;

public class CreateEmployeeCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IEmployeeCodeGenerator _codeGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateEmployeeCommandHandler(
        IRepository<Employee, Guid> employeeRepository,
        IRepository<Department, Guid> departmentRepository,
        IEmployeeCodeGenerator codeGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _codeGenerator = codeGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Hr.DTOs.EmployeeDto>> Handle(
        CreateEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Validate email uniqueness
        var emailSpec = new Specifications.EmployeeByEmailSpec(command.Email, tenantId);
        var existingByEmail = await _employeeRepository.FirstOrDefaultAsync(emailSpec, cancellationToken);
        if (existingByEmail is not null)
        {
            return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                Error.Conflict($"An employee with email '{command.Email}' already exists.", "NOIR-HR-001"));
        }

        // Validate department exists
        var deptSpec = new Specifications.DepartmentByIdSpec(command.DepartmentId);
        var department = await _departmentRepository.FirstOrDefaultAsync(deptSpec, cancellationToken);
        if (department is null)
        {
            return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                Error.NotFound($"Department with ID '{command.DepartmentId}' not found.", "NOIR-HR-002"));
        }

        // Validate manager exists if specified
        if (command.ManagerId.HasValue)
        {
            var managerSpec = new Specifications.EmployeeByIdSpec(command.ManagerId.Value);
            var manager = await _employeeRepository.FirstOrDefaultAsync(managerSpec, cancellationToken);
            if (manager is null)
            {
                return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                    Error.NotFound($"Manager with ID '{command.ManagerId}' not found.", "NOIR-HR-003"));
            }
        }

        // Validate user link uniqueness if specified
        if (!string.IsNullOrWhiteSpace(command.UserId))
        {
            var userSpec = new Specifications.EmployeeByUserIdSpec(command.UserId);
            var existingByUser = await _employeeRepository.FirstOrDefaultAsync(userSpec, cancellationToken);
            if (existingByUser is not null)
            {
                return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                    Error.Conflict($"An employee is already linked to user '{command.UserId}'.", "NOIR-HR-004"));
            }
        }

        // Generate employee code
        var employeeCode = await _codeGenerator.GenerateNextAsync(tenantId, cancellationToken);

        // Create employee
        var employee = Employee.Create(
            employeeCode,
            command.FirstName,
            command.LastName,
            command.Email,
            command.DepartmentId,
            command.JoinDate,
            command.EmploymentType,
            tenantId,
            command.Phone,
            command.Position,
            command.ManagerId,
            command.UserId,
            command.Notes);

        await _employeeRepository.AddAsync(employee, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Employee",
            entityId: employee.Id,
            operation: EntityOperation.Created,
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
