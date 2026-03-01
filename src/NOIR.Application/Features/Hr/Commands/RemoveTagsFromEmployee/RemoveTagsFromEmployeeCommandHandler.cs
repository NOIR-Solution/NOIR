namespace NOIR.Application.Features.Hr.Commands.RemoveTagsFromEmployee;

public class RemoveTagsFromEmployeeCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveTagsFromEmployeeCommandHandler(
        IRepository<Employee, Guid> employeeRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _employeeRepository = employeeRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<TagBriefDto>>> Handle(
        RemoveTagsFromEmployeeCommand command,
        CancellationToken cancellationToken)
    {
        // Validate employee exists
        var employeeExists = await _employeeRepository.ExistsAsync(command.EmployeeId, cancellationToken);
        if (!employeeExists)
        {
            return Result.Failure<List<TagBriefDto>>(
                Error.NotFound($"Employee with ID '{command.EmployeeId}' not found.", "NOIR-HR-036"));
        }

        // Find existing assignments to remove
        var assignments = await _dbContext.EmployeeTagAssignments
            .Where(a => a.EmployeeId == command.EmployeeId && command.TagIds.Contains(a.EmployeeTagId))
            .ToListAsync(cancellationToken);

        if (assignments.Count > 0)
        {
            _dbContext.EmployeeTagAssignments.RemoveRange(assignments);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // Return remaining tags
        var remainingAssignments = await _dbContext.EmployeeTagAssignments
            .Where(a => a.EmployeeId == command.EmployeeId)
            .Include(a => a.EmployeeTag!)
            .ToListAsync(cancellationToken);

        var result = remainingAssignments
            .Where(a => a.EmployeeTag is not null)
            .Select(a => new TagBriefDto(a.EmployeeTag!.Id, a.EmployeeTag.Name, a.EmployeeTag.Category, a.EmployeeTag.Color))
            .ToList();

        return Result.Success(result);
    }
}
