namespace NOIR.Application.Features.Hr.Commands.LinkEmployeeToUser;

public class LinkEmployeeToUserCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public LinkEmployeeToUserCommandHandler(
        IRepository<Employee, Guid> employeeRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Hr.DTOs.EmployeeDto>> Handle(
        LinkEmployeeToUserCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.EmployeeByIdSpec(command.EmployeeId);
        var employee = await _employeeRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (employee is null)
        {
            return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                Error.NotFound($"Employee with ID '{command.EmployeeId}' not found.", "NOIR-HR-010"));
        }

        // Check that no other employee is linked to this user
        var userSpec = new Specifications.EmployeeByUserIdSpec(command.TargetUserId, command.EmployeeId);
        var existing = await _employeeRepository.FirstOrDefaultAsync(userSpec, cancellationToken);
        if (existing is not null)
        {
            return Result.Failure<Features.Hr.DTOs.EmployeeDto>(
                Error.Conflict($"Another employee is already linked to user '{command.TargetUserId}'.", "NOIR-HR-004"));
        }

        employee.LinkToUser(command.TargetUserId);
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
            employee.ManagerId,
            employee.Manager != null ? $"{employee.Manager.FirstName} {employee.Manager.LastName}" : null,
            employee.UserId, employee.UserId != null,
            employee.JoinDate, employee.EndDate, employee.Status, employee.EmploymentType,
            employee.Notes, [], [], employee.CreatedAt, employee.ModifiedAt));
    }
}
