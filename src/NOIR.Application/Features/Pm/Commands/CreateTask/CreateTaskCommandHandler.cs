namespace NOIR.Application.Features.Pm.Commands.CreateTask;

public class CreateTaskCommandHandler
{
    private readonly IRepository<ProjectTask, Guid> _taskRepository;
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ITaskNumberGenerator _taskNumberGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateTaskCommandHandler(
        IRepository<ProjectTask, Guid> taskRepository,
        IRepository<Project, Guid> projectRepository,
        IRepository<Employee, Guid> employeeRepository,
        IApplicationDbContext dbContext,
        ITaskNumberGenerator taskNumberGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _employeeRepository = employeeRepository;
        _dbContext = dbContext;
        _taskNumberGenerator = taskNumberGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Pm.DTOs.TaskDto>> Handle(
        CreateTaskCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Verify project exists
        var project = await _projectRepository.GetByIdAsync(command.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskDto>(
                Error.NotFound($"Project with ID '{command.ProjectId}' not found.", "NOIR-PM-002"));
        }

        // Determine column (use specified or first column)
        Guid? columnId = command.ColumnId;
        if (!columnId.HasValue)
        {
            var firstColumn = await _dbContext.ProjectColumns
                .Where(c => c.ProjectId == command.ProjectId)
                .OrderBy(c => c.SortOrder)
                .TagWith("CreateTask_FirstColumn")
                .FirstOrDefaultAsync(cancellationToken);
            columnId = firstColumn?.Id;
        }

        // Assign sort order: place new task at the end of its column
        double sortOrder = 0;
        if (columnId.HasValue)
        {
            var maxSortOrder = await _dbContext.ProjectTasks
                .Where(t => t.ColumnId == columnId && !t.IsArchived)
                .TagWith("CreateTask_MaxSortOrder")
                .Select(t => (double?)t.SortOrder)
                .MaxAsync(cancellationToken);
            sortOrder = (maxSortOrder ?? 0) + 1;
        }

        // Find reporter (current user's employee record)
        Guid? reporterId = null;
        if (_currentUser.UserId is not null)
        {
            var employeeSpec = new Features.Hr.Specifications.EmployeeByUserIdSpec(_currentUser.UserId);
            var employee = await _employeeRepository.FirstOrDefaultAsync(employeeSpec, cancellationToken);
            reporterId = employee?.Id;
        }

        // Generate task number
        var taskNumber = await _taskNumberGenerator.GenerateNextAsync(project.Slug.ToUpperInvariant(), tenantId, cancellationToken);

        var task = ProjectTask.Create(
            command.ProjectId,
            taskNumber,
            command.Title,
            tenantId,
            command.Description,
            command.Priority ?? TaskPriority.Medium,
            command.AssigneeId,
            reporterId,
            command.DueDate,
            command.EstimatedHours,
            command.ParentTaskId,
            columnId,
            sortOrder);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProjectTask",
            entityId: task.Id,
            operation: EntityOperation.Created,
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
