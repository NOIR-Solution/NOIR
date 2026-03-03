namespace NOIR.Application.Features.Hr.Commands.DeleteDepartment;

public class DeleteDepartmentCommandHandler
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteDepartmentCommandHandler(
        IRepository<Department, Guid> departmentRepository,
        IRepository<Employee, Guid> employeeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<bool>> Handle(
        DeleteDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.DepartmentByIdSpec(command.Id);
        var department = await _departmentRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (department is null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Department with ID '{command.Id}' not found.", "NOIR-HR-022"));
        }

        // Check for active employees
        var employeesSpec = new Specifications.EmployeesByDepartmentSpec(command.Id);
        var hasEmployees = await _employeeRepository.AnyAsync(employeesSpec, cancellationToken);
        if (hasEmployees)
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete a department that has employees. Please reassign employees first.", "NOIR-HR-023"));
        }

        // Check for active sub-departments
        var subDeptsSpec = new Specifications.ActiveSubDepartmentsSpec(command.Id);
        var hasSubDepts = await _departmentRepository.AnyAsync(subDeptsSpec, cancellationToken);
        if (hasSubDepts)
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete a department that has active sub-departments.", "NOIR-HR-024"));
        }

        _departmentRepository.Remove(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Department",
            entityId: command.Id,
            operation: EntityOperation.Deleted,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(true);
    }
}
