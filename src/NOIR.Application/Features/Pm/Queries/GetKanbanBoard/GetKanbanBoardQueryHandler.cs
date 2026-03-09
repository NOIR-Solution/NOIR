namespace NOIR.Application.Features.Pm.Queries.GetKanbanBoard;

public class GetKanbanBoardQueryHandler
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<ProjectTask, Guid> _taskRepository;

    public GetKanbanBoardQueryHandler(
        IRepository<Project, Guid> projectRepository,
        IApplicationDbContext dbContext,
        IRepository<ProjectTask, Guid> taskRepository)
    {
        _projectRepository = projectRepository;
        _dbContext = dbContext;
        _taskRepository = taskRepository;
    }

    public async Task<Result<Features.Pm.DTOs.KanbanBoardDto>> Handle(
        GetKanbanBoardQuery query,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdAsync(query.ProjectId, cancellationToken);
        if (project is null)
        {
            return Result.Failure<Features.Pm.DTOs.KanbanBoardDto>(
                Error.NotFound($"Project with ID '{query.ProjectId}' not found.", "NOIR-PM-002"));
        }

        // Fetch columns
        var columns = await _dbContext.ProjectColumns
            .Where(c => c.ProjectId == query.ProjectId)
            .OrderBy(c => c.SortOrder)
            .TagWith("GetKanbanBoard_Columns")
            .ToListAsync(cancellationToken);

        // Fetch all tasks for the project with card-level data
        var tasksSpec = new Specifications.TasksForKanbanSpec(query.ProjectId);
        var tasks = await _taskRepository.ListAsync(tasksSpec, cancellationToken);

        // Group tasks by column (filter out tasks with no column)
        var tasksByColumn = tasks
            .Where(t => t.ColumnId.HasValue)
            .GroupBy(t => t.ColumnId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var kanbanColumns = columns.Select(col =>
        {
            var columnTasks = tasksByColumn.GetValueOrDefault(col.Id) ?? new List<ProjectTask>();

            var taskCards = columnTasks.OrderBy(t => t.SortOrder).Select(t => new Features.Pm.DTOs.TaskCardDto(
                t.Id, t.TaskNumber, t.Title, t.Status, t.Priority,
                t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : null,
                t.Assignee?.AvatarUrl,
                t.DueDate,
                t.Comments.Count,
                t.SubTasks.Count,
                t.SubTasks.Count(s => s.Status == ProjectTaskStatus.Done),
                t.TaskLabels.Select(tl => new Features.Pm.DTOs.TaskLabelBriefDto(
                    tl.Label!.Id, tl.Label.Name, tl.Label.Color)).ToList(),
                t.SortOrder,
                t.ParentTaskId, t.ParentTask?.TaskNumber)).ToList();

            return new Features.Pm.DTOs.KanbanColumnDto(
                col.Id, col.Name, col.SortOrder, col.Color, col.WipLimit, taskCards);
        }).ToList();

        return Result.Success(new Features.Pm.DTOs.KanbanBoardDto(
            project.Id, project.Name, kanbanColumns));
    }
}
