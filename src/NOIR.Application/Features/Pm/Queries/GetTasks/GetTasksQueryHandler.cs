namespace NOIR.Application.Features.Pm.Queries.GetTasks;

public class GetTasksQueryHandler
{
    private readonly IRepository<ProjectTask, Guid> _taskRepository;

    public GetTasksQueryHandler(IRepository<ProjectTask, Guid> taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<Result<PagedResult<Features.Pm.DTOs.TaskCardDto>>> Handle(
        GetTasksQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        var spec = new Specifications.TasksByProjectSpec(
            query.ProjectId, query.Status, query.Priority,
            query.AssigneeId, query.Search, skip, query.PageSize);

        var tasks = await _taskRepository.ListAsync(spec, cancellationToken);

        var countSpec = new Specifications.TasksByProjectCountSpec(
            query.ProjectId, query.Status, query.Priority,
            query.AssigneeId, query.Search);
        var totalCount = await _taskRepository.CountAsync(countSpec, cancellationToken);

        var items = tasks.Select(t => new Features.Pm.DTOs.TaskCardDto(
            t.Id, t.TaskNumber, t.Title, t.Status, t.Priority,
            t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : null,
            t.Assignee?.AvatarUrl,
            t.DueDate,
            0, // Comment count not loaded in list spec for performance
            0, // Subtask count
            0, // Completed subtask count
            t.TaskLabels.Select(tl => new Features.Pm.DTOs.TaskLabelBriefDto(
                tl.Label!.Id, tl.Label.Name, tl.Label.Color)).ToList(),
            t.SortOrder,
            t.ParentTaskId, t.ParentTask?.TaskNumber)).ToList();

        return Result.Success(PagedResult<Features.Pm.DTOs.TaskCardDto>.Create(
            items, totalCount, query.Page - 1, query.PageSize));
    }
}
