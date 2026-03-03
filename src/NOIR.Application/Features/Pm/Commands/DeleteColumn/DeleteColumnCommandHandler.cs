namespace NOIR.Application.Features.Pm.Commands.DeleteColumn;

public class DeleteColumnCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<ProjectTask, Guid> _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteColumnCommandHandler(
        IApplicationDbContext dbContext,
        IRepository<ProjectTask, Guid> taskRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _dbContext = dbContext;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Pm.DTOs.ProjectColumnDto>> Handle(
        DeleteColumnCommand command,
        CancellationToken cancellationToken)
    {
        var column = await _dbContext.ProjectColumns
            .TagWith("DeleteColumn_Fetch")
            .FirstOrDefaultAsync(c => c.Id == command.ColumnId, cancellationToken);

        if (column is null)
        {
            return Result.Failure<Features.Pm.DTOs.ProjectColumnDto>(
                Error.NotFound($"Column with ID '{command.ColumnId}' not found.", "NOIR-PM-013"));
        }

        if (command.ColumnId == command.MoveToColumnId)
        {
            return Result.Failure<Features.Pm.DTOs.ProjectColumnDto>(
                Error.Validation("MoveToColumnId", "Cannot move tasks to the same column being deleted.", "NOIR-PM-014"));
        }

        // Verify target column exists
        var targetColumn = await _dbContext.ProjectColumns
            .TagWith("DeleteColumn_FetchTarget")
            .FirstOrDefaultAsync(c => c.Id == command.MoveToColumnId, cancellationToken);

        if (targetColumn is null)
        {
            return Result.Failure<Features.Pm.DTOs.ProjectColumnDto>(
                Error.NotFound($"Target column with ID '{command.MoveToColumnId}' not found.", "NOIR-PM-013"));
        }

        // Move all tasks from deleted column to target column
        var tasksSpec = new Specifications.TasksByColumnSpec(command.ColumnId);
        var tasks = await _taskRepository.ListAsync(tasksSpec, cancellationToken);
        foreach (var task in tasks)
        {
            // Get tracked version
            var taskUpdateSpec = new Specifications.TaskByIdForUpdateSpec(task.Id);
            var tracked = await _taskRepository.FirstOrDefaultAsync(taskUpdateSpec, cancellationToken);
            tracked?.MoveToColumn(command.MoveToColumnId, tracked.SortOrder);
        }

        var dto = new Features.Pm.DTOs.ProjectColumnDto(
            column.Id, column.Name, column.SortOrder, column.Color, column.WipLimit,
            column.StatusMapping);

        _dbContext.ProjectColumns.Remove(column);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProjectColumn",
            entityId: command.ColumnId,
            operation: EntityOperation.Deleted,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(dto);
    }
}
