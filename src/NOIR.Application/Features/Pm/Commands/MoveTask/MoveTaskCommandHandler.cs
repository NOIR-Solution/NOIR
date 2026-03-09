namespace NOIR.Application.Features.Pm.Commands.MoveTask;

public class MoveTaskCommandHandler
{
    private readonly IRepository<ProjectTask, Guid> _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public MoveTaskCommandHandler(
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

    public async Task<Result<Features.Pm.DTOs.TaskDto>> Handle(
        MoveTaskCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.TaskByIdForUpdateSpec(command.Id);
        var task = await _taskRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (task is null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskDto>(
                Error.NotFound($"Task with ID '{command.Id}' not found.", "NOIR-PM-006"));
        }

        task.MoveToColumn(command.ColumnId, command.SortOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProjectTask",
            entityId: task.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        // Reload with navigation properties
        var reloadSpec = new Specifications.TaskByIdSpec(task.Id);
        var reloaded = await _taskRepository.FirstOrDefaultAsync(reloadSpec, cancellationToken);

        return Result.Success(MapToDto(reloaded!));
    }

    private static Features.Pm.DTOs.TaskDto MapToDto(ProjectTask t) =>
        new(t.Id, t.ProjectId, t.TaskNumber, t.Title, t.Description,
            t.Status, t.Priority,
            t.AssigneeId, t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : null,
            t.ReporterId, t.Reporter != null ? $"{t.Reporter.FirstName} {t.Reporter.LastName}" : null,
            t.DueDate, t.EstimatedHours, t.ActualHours,
            t.ParentTaskId, t.ParentTask?.TaskNumber, t.ParentTask?.Title,
            t.ColumnId, t.Column?.Name,
            t.CompletedAt,
            t.TaskLabels.Select(tl => new Features.Pm.DTOs.TaskLabelBriefDto(
                tl.Label!.Id, tl.Label.Name, tl.Label.Color)).ToList(),
            t.SubTasks.Select(s => new Features.Pm.DTOs.SubtaskDto(
                s.Id, s.TaskNumber, s.Title, s.Status, s.Priority,
                s.Assignee != null ? $"{s.Assignee.FirstName} {s.Assignee.LastName}" : null)).ToList(),
            t.Comments.OrderByDescending(c => c.CreatedAt).Select(c => new Features.Pm.DTOs.TaskCommentDto(
                c.Id, c.AuthorId,
                c.Author != null ? $"{c.Author.FirstName} {c.Author.LastName}" : string.Empty,
                c.Author?.AvatarUrl,
                c.Content, c.IsEdited, c.CreatedAt)).ToList(),
            t.CreatedAt, t.ModifiedAt,
            t.Project?.Name, t.Assignee?.AvatarUrl);
}
