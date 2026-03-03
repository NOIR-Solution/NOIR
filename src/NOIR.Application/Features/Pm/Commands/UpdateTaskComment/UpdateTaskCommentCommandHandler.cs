namespace NOIR.Application.Features.Pm.Commands.UpdateTaskComment;

public class UpdateTaskCommentCommandHandler
{
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateTaskCommentCommandHandler(
        IRepository<Employee, Guid> employeeRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _employeeRepository = employeeRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Pm.DTOs.TaskCommentDto>> Handle(
        UpdateTaskCommentCommand command,
        CancellationToken cancellationToken)
    {
        var comment = await _dbContext.TaskComments
            .Where(c => c.Id == command.CommentId && c.TaskId == command.TaskId)
            .TagWith("UpdateTaskComment_Fetch")
            .FirstOrDefaultAsync(cancellationToken);

        if (comment is null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskCommentDto>(
                Error.NotFound($"Comment with ID '{command.CommentId}' not found.", "NOIR-PM-011"));
        }

        // Verify current user is the author
        Employee? currentEmployee = null;
        if (_currentUser.UserId is not null)
        {
            var employeeSpec = new Features.Hr.Specifications.EmployeeByUserIdSpec(_currentUser.UserId);
            currentEmployee = await _employeeRepository.FirstOrDefaultAsync(employeeSpec, cancellationToken);
        }

        if (currentEmployee is null || currentEmployee.Id != comment.AuthorId)
        {
            return Result.Failure<Features.Pm.DTOs.TaskCommentDto>(
                Error.Forbidden("You can only edit your own comments.", "NOIR-PM-012"));
        }

        comment.Edit(command.Content);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProjectTask",
            entityId: command.TaskId,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new Features.Pm.DTOs.TaskCommentDto(
            comment.Id, currentEmployee.Id,
            $"{currentEmployee.FirstName} {currentEmployee.LastName}",
            currentEmployee.AvatarUrl,
            comment.Content, comment.IsEdited, comment.CreatedAt));
    }
}
