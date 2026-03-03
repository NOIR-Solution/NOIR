namespace NOIR.Application.Features.Pm.Commands.AddTaskComment;

public class AddTaskCommentCommandHandler
{
    private readonly IRepository<ProjectTask, Guid> _taskRepository;
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public AddTaskCommentCommandHandler(
        IRepository<ProjectTask, Guid> taskRepository,
        IRepository<Employee, Guid> employeeRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _taskRepository = taskRepository;
        _employeeRepository = employeeRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Pm.DTOs.TaskCommentDto>> Handle(
        AddTaskCommentCommand command,
        CancellationToken cancellationToken)
    {
        // Verify task exists
        var task = await _taskRepository.GetByIdAsync(command.TaskId, cancellationToken);
        if (task is null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskCommentDto>(
                Error.NotFound($"Task with ID '{command.TaskId}' not found.", "NOIR-PM-006"));
        }

        // Get current employee
        Employee? author = null;
        if (_currentUser.UserId is not null)
        {
            var employeeSpec = new Features.Hr.Specifications.EmployeeByUserIdSpec(_currentUser.UserId);
            author = await _employeeRepository.FirstOrDefaultAsync(employeeSpec, cancellationToken);
        }

        if (author is null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskCommentDto>(
                Error.Validation("AuthorId", "Current user is not linked to an employee.", "NOIR-PM-007"));
        }

        var comment = TaskComment.Create(command.TaskId, author.Id, command.Content, _currentUser.TenantId);
        _dbContext.TaskComments.Add(comment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProjectTask",
            entityId: command.TaskId,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new Features.Pm.DTOs.TaskCommentDto(
            comment.Id, author.Id,
            $"{author.FirstName} {author.LastName}",
            author.AvatarUrl,
            comment.Content, comment.IsEdited, comment.CreatedAt));
    }
}
