namespace NOIR.Application.Features.Pm.Commands.AddSubtask;

public class AddSubtaskCommandHandler
{
    private const int MaxSubtaskDepth = 3;

    private readonly IRepository<ProjectTask, Guid> _taskRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly ITaskNumberGenerator _taskNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public AddSubtaskCommandHandler(
        IRepository<ProjectTask, Guid> taskRepository,
        IRepository<Project, Guid> projectRepository,
        ITaskNumberGenerator taskNumberGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _taskNumberGenerator = taskNumberGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Pm.DTOs.TaskDto>> Handle(
        AddSubtaskCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Fetch parent task
        var parentSpec = new Specifications.TaskByIdSpec(command.ParentTaskId);
        var parentTask = await _taskRepository.FirstOrDefaultAsync(parentSpec, cancellationToken);
        if (parentTask is null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskDto>(
                Error.NotFound($"Parent task with ID '{command.ParentTaskId}' not found.", "NOIR-PM-006"));
        }

        // Validate depth: walk up ParentTaskId chain
        var depth = 1; // current subtask will be at depth 1 from parent
        var currentParentId = parentTask.ParentTaskId;
        while (currentParentId.HasValue)
        {
            depth++;
            if (depth > MaxSubtaskDepth)
            {
                return Result.Failure<Features.Pm.DTOs.TaskDto>(
                    Error.Validation("ParentTaskId",
                        $"Maximum subtask depth of {MaxSubtaskDepth} exceeded.",
                        "NOIR-PM-022"));
            }

            var ancestor = await _taskRepository.GetByIdAsync(currentParentId.Value, cancellationToken);
            currentParentId = ancestor?.ParentTaskId;
        }

        // Get project for task number generation
        var project = await _projectRepository.GetByIdAsync(parentTask.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskDto>(
                Error.NotFound($"Project with ID '{parentTask.ProjectId}' not found.", "NOIR-PM-002"));
        }

        // Generate task number
        var taskNumber = await _taskNumberGenerator.GenerateNextAsync(
            project.Slug.ToUpperInvariant(), tenantId, cancellationToken);

        var subtask = ProjectTask.Create(
            parentTask.ProjectId,
            taskNumber,
            command.Title,
            tenantId,
            command.Description,
            command.Priority,
            command.AssigneeId,
            parentTaskId: command.ParentTaskId,
            columnId: parentTask.ColumnId);

        await _taskRepository.AddAsync(subtask, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProjectTask",
            entityId: subtask.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        // Reload with navigation properties
        var reloadSpec = new Specifications.TaskByIdSpec(subtask.Id);
        var reloaded = await _taskRepository.FirstOrDefaultAsync(reloadSpec, cancellationToken);

        return Result.Success(MapToDto(reloaded!));
    }

    private static Features.Pm.DTOs.TaskDto MapToDto(ProjectTask t) =>
        new(t.Id, t.ProjectId, t.TaskNumber, t.Title, t.Description,
            t.Status, t.Priority,
            t.AssigneeId, t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : null,
            t.ReporterId, t.Reporter != null ? $"{t.Reporter.FirstName} {t.Reporter.LastName}" : null,
            t.DueDate, t.EstimatedHours, t.ActualHours,
            t.ParentTaskId, t.ParentTask?.TaskNumber,
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
