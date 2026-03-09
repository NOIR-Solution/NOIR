namespace NOIR.Application.Features.Pm.Queries.GetArchivedTasks;

public class GetArchivedTasksQueryHandler
{
    private readonly IRepository<ProjectTask, Guid> _taskRepository;

    public GetArchivedTasksQueryHandler(IRepository<ProjectTask, Guid> taskRepository)
    {
        _taskRepository = taskRepository;
    }

    public async Task<Result<List<Features.Pm.DTOs.ArchivedTaskCardDto>>> Handle(
        GetArchivedTasksQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.ArchivedTasksByProjectReadSpec(query.ProjectId);
        var tasks = await _taskRepository.ListAsync(spec, cancellationToken);

        var dtos = tasks.Select(t => new Features.Pm.DTOs.ArchivedTaskCardDto(
            t.Id,
            t.TaskNumber,
            t.Title,
            t.Status,
            t.Priority,
            t.Assignee != null ? $"{t.Assignee.FirstName} {t.Assignee.LastName}" : null,
            t.Assignee?.AvatarUrl,
            t.ArchivedAt,
            t.SubTasks.Count,
            t.TaskLabels.Select(tl => new Features.Pm.DTOs.TaskLabelBriefDto(
                tl.Label!.Id, tl.Label.Name, tl.Label.Color)).ToList(),
            t.ParentTaskId,
            t.ParentTask?.TaskNumber)).ToList();

        return Result.Success(dtos);
    }
}
