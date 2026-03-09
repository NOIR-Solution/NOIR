namespace NOIR.Application.Features.Pm.Commands.EmptyProjectTrash;

public class EmptyProjectTrashCommandHandler
{
    private readonly IRepository<ProjectTask, Guid> _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public EmptyProjectTrashCommandHandler(
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

    public async Task<Result<int>> Handle(
        EmptyProjectTrashCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.ArchivedTasksByProjectSpec(command.ProjectId);
        var archivedTasks = await _taskRepository.ListAsync(spec, cancellationToken);

        if (archivedTasks.Count == 0)
            return Result.Success(0);

        foreach (var task in archivedTasks)
            _taskRepository.Remove(task);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var task in archivedTasks)
        {
            await _entityUpdateHub.PublishEntityUpdatedAsync(
                entityType: "ProjectTask",
                entityId: task.Id,
                operation: EntityOperation.Deleted,
                tenantId: _currentUser.TenantId!,
                cancellationToken);
        }

        return Result.Success(archivedTasks.Count);
    }
}
