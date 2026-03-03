namespace NOIR.Application.Features.Hr.Commands.UpdateDepartment;

public class UpdateDepartmentCommandHandler
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateDepartmentCommandHandler(
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
        UpdateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        var spec = new Specifications.DepartmentByIdSpec(command.Id);
        var department = await _departmentRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (department is null)
        {
            return Result.Failure<Features.Hr.DTOs.DepartmentDto>(
                Error.NotFound($"Department with ID '{command.Id}' not found.", "NOIR-HR-022"));
        }

        // Validate code uniqueness (excluding self)
        if (department.Code != command.Code.Trim().ToUpperInvariant())
        {
            var codeSpec = new Specifications.DepartmentByCodeSpec(command.Code, tenantId, command.Id);
            var existingByCode = await _departmentRepository.FirstOrDefaultAsync(codeSpec, cancellationToken);
            if (existingByCode is not null)
            {
                return Result.Failure<Features.Hr.DTOs.DepartmentDto>(
                    Error.Conflict($"A department with code '{command.Code}' already exists.", "NOIR-HR-020"));
            }
        }

        // Validate parent chain (no circular reference)
        if (command.ParentDepartmentId.HasValue)
        {
            if (command.ParentDepartmentId.Value == command.Id)
            {
                return Result.Failure<Features.Hr.DTOs.DepartmentDto>(
                    Error.Validation("ParentDepartmentId", "A department cannot be its own parent."));
            }

            // Walk up the chain to check for circular reference
            var currentParentId = command.ParentDepartmentId;
            var visited = new HashSet<Guid> { command.Id };
            while (currentParentId.HasValue)
            {
                if (!visited.Add(currentParentId.Value))
                {
                    return Result.Failure<Features.Hr.DTOs.DepartmentDto>(
                        Error.Validation("ParentDepartmentId", "Setting this parent would create a circular department hierarchy."));
                }

                var parentSpec = new Specifications.DepartmentByIdSpec(currentParentId.Value);
                var parent = await _departmentRepository.FirstOrDefaultAsync(parentSpec, cancellationToken);
                if (parent is null) break;
                currentParentId = parent.ParentDepartmentId;
            }
        }

        department.Update(
            command.Name,
            command.Code,
            command.Description,
            command.ManagerId,
            command.ParentDepartmentId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Department",
            entityId: department.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new Features.Hr.DTOs.DepartmentDto(
            department.Id, department.Name, department.Code, department.Description,
            department.ManagerId, null,
            department.ParentDepartmentId, null,
            department.SortOrder, department.IsActive, 0, []));
    }
}
