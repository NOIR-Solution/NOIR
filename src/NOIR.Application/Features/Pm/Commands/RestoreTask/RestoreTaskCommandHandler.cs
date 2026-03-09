namespace NOIR.Application.Features.Pm.Commands.RestoreTask;

public class RestoreTaskCommandHandler
{
    private readonly IRepository<ProjectTask, Guid> _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public RestoreTaskCommandHandler(
        IRepository<ProjectTask, Guid> taskRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Guid>> Handle(
        RestoreTaskCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.TaskByIdForUpdateSpec(command.Id);
        var task = await _taskRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (task is null)
            return Result.Failure<Guid>(Error.NotFound($"Task with ID '{command.Id}' not found.", "NOIR-PM-006"));

        if (!task.IsArchived)
            return Result.Failure<Guid>(Error.Validation("IsArchived", "Task is not archived.", "NOIR-PM-020"));

        task.Restore();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProjectTask",
            entityId: command.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(command.Id);
    }
}
