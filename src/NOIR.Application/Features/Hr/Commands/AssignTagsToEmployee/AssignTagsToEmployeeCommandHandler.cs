namespace NOIR.Application.Features.Hr.Commands.AssignTagsToEmployee;

public class AssignTagsToEmployeeCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IRepository<EmployeeTag, Guid> _tagRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AssignTagsToEmployeeCommandHandler(
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

    public async Task<Result<List<TagBriefDto>>> Handle(
        AssignTagsToEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Validate employee exists
        var employeeExists = await _employeeRepository.ExistsAsync(command.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return Result.Failure<List<TagBriefDto>>(
                Error.NotFound($"Employee with ID '{command.EmployeeId}' not found.", "NOIR-HR-034"));
        }

        // Validate all tags exist
        var tagsSpec = new EmployeeTagsByIdsSpec(command.TagIds);
        var foundTags = await _tagRepository.ListAsync(tagsSpec, cancellationToken);
        if (foundTags.Count != command.TagIds.Count)
        {
            var foundIds = foundTags.Select(t => t.Id).ToHashSet();
            var missingId = command.TagIds.First(id => !foundIds.Contains(id));
            return Result.Failure<List<TagBriefDto>>(
                Error.NotFound($"Employee tag with ID '{missingId}' not found.", "NOIR-HR-035"));
        }

        // Get existing assignments to avoid duplicates
        var existingTagIds = await _dbContext.EmployeeTagAssignments
            .Where(a => a.EmployeeId == command.EmployeeId && command.TagIds.Contains(a.EmployeeTagId))
            .Select(a => a.EmployeeTagId)
            .ToListAsync(cancellationToken);

        var existingSet = existingTagIds.ToHashSet();
        var newTagIds = command.TagIds.Where(id => !existingSet.Contains(id)).ToList();

        // Create new assignments
        foreach (var tagId in newTagIds)
        {
            var assignment = EmployeeTagAssignment.Create(command.EmployeeId, tagId, tenantId);
            _dbContext.EmployeeTagAssignments.Add(assignment);
        }

        if (newTagIds.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Return all current tags for the employee
        var allAssignments = await _dbContext.EmployeeTagAssignments
            .Where(a => a.EmployeeId == command.EmployeeId)
            .Include(a => a.EmployeeTag!)
            .ToListAsync(cancellationToken);

        var result = allAssignments
            .Where(a => a.EmployeeTag is not null)
            .Select(a => new TagBriefDto(a.EmployeeTag!.Id, a.EmployeeTag.Name, a.EmployeeTag.Category, a.EmployeeTag.Color))
            .ToList();

        return Result.Success(result);
    }
}
