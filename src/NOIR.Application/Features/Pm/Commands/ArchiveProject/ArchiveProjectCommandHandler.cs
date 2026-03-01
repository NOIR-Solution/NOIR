namespace NOIR.Application.Features.Pm.Commands.ArchiveProject;

public class ArchiveProjectCommandHandler
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ArchiveProjectCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Features.Pm.DTOs.ProjectDto>> Handle(
        ArchiveProjectCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.ProjectByIdForUpdateSpec(command.Id);
        var project = await _projectRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (project is null)
        {
            return Result.Failure<Features.Pm.DTOs.ProjectDto>(
                Error.NotFound($"Project with ID '{command.Id}' not found.", "NOIR-PM-002"));
        }

        project.Archive();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload with navigation properties
        var reloadSpec = new Specifications.ProjectByIdSpec(project.Id);
        var reloaded = await _projectRepository.FirstOrDefaultAsync(reloadSpec, cancellationToken);

        return Result.Success(MapToDto(reloaded!));
    }

    private static Features.Pm.DTOs.ProjectDto MapToDto(Project p)
    {
        var taskCount = p.Tasks.Count;
        var completedTaskCount = p.Tasks.Count(t => t.Status == ProjectTaskStatus.Done);
        var progressPercent = taskCount > 0 ? Math.Round((decimal)completedTaskCount / taskCount * 100, 1) : 0;

        return new(p.Id, p.Name, p.Slug, p.Description, p.Status,
            p.StartDate, p.EndDate, p.DueDate,
            p.OwnerId, p.Owner != null ? $"{p.Owner.FirstName} {p.Owner.LastName}" : null,
            p.Budget, p.Currency, p.Color, p.Icon, p.Visibility,
            p.Members.Select(m => new Features.Pm.DTOs.ProjectMemberDto(
                m.Id, m.EmployeeId,
                m.Employee != null ? $"{m.Employee.FirstName} {m.Employee.LastName}" : string.Empty,
                m.Employee?.AvatarUrl,
                m.Role, m.JoinedAt,
                m.Employee?.EmployeeCode, m.Employee?.Position)).ToList(),
            p.Columns.OrderBy(c => c.SortOrder).Select(c => new Features.Pm.DTOs.ProjectColumnDto(
                c.Id, c.Name, c.SortOrder, c.Color, c.WipLimit,
                c.StatusMapping, p.Tasks.Count(t => t.ColumnId == c.Id))).ToList(),
            p.CreatedAt, p.ModifiedAt,
            p.ProjectCode, p.Owner?.AvatarUrl,
            p.Members.Count, taskCount, completedTaskCount, progressPercent);
    }
}
