namespace NOIR.Application.Features.Hr.Commands.BulkAssignTags;

/// <summary>
/// Wolverine handler for bulk assigning tags to multiple employees.
/// </summary>
public class BulkAssignTagsCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<EmployeeTag, Guid> _tagRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public BulkAssignTagsCommandHandler(
        IRepository<Employee, Guid> employeeRepository,
        IRepository<EmployeeTag, Guid> tagRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _employeeRepository = employeeRepository;
        _tagRepository = tagRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<BulkOperationResultDto>> Handle(
        BulkAssignTagsCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;
        var successCount = 0;
        var errors = new List<BulkOperationErrorDto>();

        // Validate all tags exist
        var tagsSpec = new EmployeeTagsByIdsSpec(command.TagIds);
        var foundTags = await _tagRepository.ListAsync(tagsSpec, cancellationToken);
        if (foundTags.Count != command.TagIds.Count)
        {
            var foundIds = foundTags.Select(t => t.Id).ToHashSet();
            var missingId = command.TagIds.First(id => !foundIds.Contains(id));
            return Result.Failure<BulkOperationResultDto>(
                Error.NotFound($"Employee tag with ID '{missingId}' not found.", "NOIR-HR-040"));
        }

        // Get existing assignments for all employees+tags to avoid duplicates
        var existingAssignments = await _dbContext.EmployeeTagAssignments
            .Where(a => command.EmployeeIds.Contains(a.EmployeeId) && command.TagIds.Contains(a.EmployeeTagId))
            .Select(a => new { a.EmployeeId, a.EmployeeTagId })
            .TagWith("BulkAssignTags_ExistingAssignments")
            .ToListAsync(cancellationToken);

        var existingSet = existingAssignments
            .Select(a => (a.EmployeeId, a.EmployeeTagId))
            .ToHashSet();

        foreach (var employeeId in command.EmployeeIds)
        {
            var exists = await _employeeRepository.ExistsAsync(employeeId, cancellationToken);
            if (!exists)
            {
                errors.Add(new BulkOperationErrorDto(employeeId, null, "Employee not found"));
                continue;
            }

            var assignedCount = 0;
            foreach (var tagId in command.TagIds)
            {
                if (!existingSet.Contains((employeeId, tagId)))
                {
                    var assignment = EmployeeTagAssignment.Create(employeeId, tagId, tenantId);
                    _dbContext.EmployeeTagAssignments.Add(assignment);
                    assignedCount++;
                }
            }

            successCount++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new BulkOperationResultDto(
            successCount,
            errors.Count,
            errors));
    }
}
