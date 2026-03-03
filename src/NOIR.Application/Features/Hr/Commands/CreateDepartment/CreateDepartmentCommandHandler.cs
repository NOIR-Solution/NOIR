namespace NOIR.Application.Features.Hr.Commands.CreateDepartment;

public class CreateDepartmentCommandHandler
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateDepartmentCommandHandler(
        IRepository<Department, Guid> departmentRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Hr.DTOs.DepartmentDto>> Handle(
        CreateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Validate code uniqueness
        var codeSpec = new Specifications.DepartmentByCodeSpec(command.Code, tenantId);
        var existingByCode = await _departmentRepository.FirstOrDefaultAsync(codeSpec, cancellationToken);
        if (existingByCode is not null)
        {
            return Result.Failure<Features.Hr.DTOs.DepartmentDto>(
                Error.Conflict($"A department with code '{command.Code}' already exists.", "NOIR-HR-020"));
        }

        // Validate parent exists if specified
        string? parentName = null;
        if (command.ParentDepartmentId.HasValue)
        {
            var parentSpec = new Specifications.DepartmentByIdSpec(command.ParentDepartmentId.Value);
            var parent = await _departmentRepository.FirstOrDefaultAsync(parentSpec, cancellationToken);
            if (parent is null)
            {
                return Result.Failure<Features.Hr.DTOs.DepartmentDto>(
                    Error.NotFound($"Parent department with ID '{command.ParentDepartmentId}' not found.", "NOIR-HR-021"));
            }
            parentName = parent.Name;
        }

        var department = Department.Create(
            command.Name,
            command.Code,
            tenantId,
            command.Description,
            command.ParentDepartmentId,
            command.ManagerId);

        await _departmentRepository.AddAsync(department, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Department",
            entityId: department.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new Features.Hr.DTOs.DepartmentDto(
            department.Id, department.Name, department.Code, department.Description,
            department.ManagerId, null,
            department.ParentDepartmentId, parentName,
            department.SortOrder, department.IsActive, 0, []));
    }
}
