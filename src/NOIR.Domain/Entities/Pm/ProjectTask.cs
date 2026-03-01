namespace NOIR.Domain.Entities.Pm;

/// <summary>
/// A task within a project, displayed on the Kanban board.
/// Named ProjectTask to avoid collision with System.Threading.Tasks.Task.
/// </summary>
public class ProjectTask : TenantAggregateRoot<Guid>
{
    public Guid ProjectId { get; private set; }
    public string TaskNumber { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectTaskStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public Guid? ReporterId { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public decimal? EstimatedHours { get; private set; }
    public decimal? ActualHours { get; private set; }
    public Guid? ParentTaskId { get; private set; }
    public Guid? ColumnId { get; private set; }
    public double SortOrder { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    // Navigation properties
    public virtual Project? Project { get; private set; }
    public virtual ProjectColumn? Column { get; private set; }
    public virtual Employee? Assignee { get; private set; }
    public virtual Employee? Reporter { get; private set; }
    public virtual ProjectTask? ParentTask { get; private set; }
    public virtual ICollection<ProjectTask> SubTasks { get; private set; } = new List<ProjectTask>();
    public virtual ICollection<TaskComment> Comments { get; private set; } = new List<TaskComment>();
    public virtual ICollection<ProjectTaskLabel> TaskLabels { get; private set; } = new List<ProjectTaskLabel>();

    // Private constructor for EF Core
    private ProjectTask() : base() { }

    private ProjectTask(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new project task.
    /// </summary>
    public static ProjectTask Create(
        Guid projectId,
        string taskNumber,
        string title,
        string? tenantId,
        string? description = null,
        TaskPriority priority = TaskPriority.Medium,
        Guid? assigneeId = null,
        Guid? reporterId = null,
        DateTimeOffset? dueDate = null,
        decimal? estimatedHours = null,
        Guid? parentTaskId = null,
        Guid? columnId = null,
        double sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskNumber);

        var task = new ProjectTask(Guid.NewGuid(), tenantId)
        {
            ProjectId = projectId,
            TaskNumber = taskNumber,
            Title = title.Trim(),
            Description = description?.Trim(),
            Status = ProjectTaskStatus.Todo,
            Priority = priority,
            AssigneeId = assigneeId,
            ReporterId = reporterId,
            DueDate = dueDate,
            EstimatedHours = estimatedHours,
            ParentTaskId = parentTaskId,
            ColumnId = columnId,
            SortOrder = sortOrder
        };

        if (assigneeId.HasValue)
        {
            task.AddDomainEvent(new Events.Pm.TaskAssignedEvent(task.Id, projectId, assigneeId, title));
        }

        return task;
    }

    /// <summary>
    /// Updates task details.
    /// </summary>
    public void Update(
        string title,
        string? description,
        TaskPriority priority,
        Guid? assigneeId,
        Guid? reporterId,
        DateTimeOffset? dueDate,
        decimal? estimatedHours,
        decimal? actualHours)
    {
        var previousAssigneeId = AssigneeId;

        Title = title.Trim();
        Description = description?.Trim();
        Priority = priority;
        AssigneeId = assigneeId;
        ReporterId = reporterId;
        DueDate = dueDate;
        EstimatedHours = estimatedHours;
        ActualHours = actualHours;

        if (assigneeId != previousAssigneeId && assigneeId.HasValue)
        {
            AddDomainEvent(new Events.Pm.TaskAssignedEvent(Id, ProjectId, assigneeId, Title));
        }
    }

    /// <summary>
    /// Moves the task to a different column with a new sort order.
    /// </summary>
    public void MoveToColumn(Guid columnId, double sortOrder)
    {
        var oldColumnId = ColumnId;
        ColumnId = columnId;
        SortOrder = sortOrder;

        if (oldColumnId != columnId)
        {
            AddDomainEvent(new Events.Pm.TaskMovedEvent(Id, oldColumnId, columnId));
        }
    }

    /// <summary>
    /// Changes the task status.
    /// </summary>
    public void ChangeStatus(ProjectTaskStatus status)
    {
        Status = status;

        if (status == ProjectTaskStatus.Done)
        {
            CompletedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new Events.Pm.TaskCompletedEvent(Id, ProjectId, Title));
        }
        else
        {
            CompletedAt = null;
        }
    }

    /// <summary>
    /// Marks the task as completed.
    /// </summary>
    public void Complete()
    {
        if (Status == ProjectTaskStatus.Done)
        {
            throw new InvalidOperationException("Task is already completed.");
        }

        ChangeStatus(ProjectTaskStatus.Done);
    }

    /// <summary>
    /// Cancels the task.
    /// </summary>
    public void Cancel()
    {
        if (Status == ProjectTaskStatus.Cancelled)
        {
            throw new InvalidOperationException("Task is already cancelled.");
        }

        Status = ProjectTaskStatus.Cancelled;
    }
}
