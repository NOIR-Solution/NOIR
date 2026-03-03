namespace NOIR.Application.Features.Pm.Commands.ChangeProjectStatus;

public class ChangeProjectStatusCommandHandler
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public ChangeProjectStatusCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Pm.DTOs.ProjectDto>> Handle(
        ChangeProjectStatusCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.ProjectByIdForUpdateSpec(command.ProjectId);
        var project = await _projectRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (project is null)
        {
            return Result.Failure<Features.Pm.DTOs.ProjectDto>(
                Error.NotFound($"Project with ID '{command.ProjectId}' not found.", "NOIR-PM-002"));
        }

        // Validate state transitions
        var validationError = ValidateTransition(project.Status, command.NewStatus);
        if (validationError is not null)
        {
            return Result.Failure<Features.Pm.DTOs.ProjectDto>(validationError);
        }

        project.ChangeStatus(command.NewStatus);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Project",
            entityId: project.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        // Reload with navigation properties
        var reloadSpec = new Specifications.ProjectByIdSpec(project.Id);
        var reloaded = await _projectRepository.FirstOrDefaultAsync(reloadSpec, cancellationToken);

        return Result.Success(MapToDto(reloaded!));
    }

    private static Error? ValidateTransition(ProjectStatus current, ProjectStatus target)
    {
        if (current == target)
        {
            return Error.Validation("NewStatus", $"Project is already in '{current}' status.", "NOIR-PM-020");
        }

        var allowed = current switch
        {
            ProjectStatus.Active => new[] { ProjectStatus.OnHold, ProjectStatus.Completed, ProjectStatus.Archived },
            ProjectStatus.OnHold => new[] { ProjectStatus.Active, ProjectStatus.Archived },
            ProjectStatus.Completed => new[] { ProjectStatus.Active, ProjectStatus.Archived },
            ProjectStatus.Archived => Array.Empty<ProjectStatus>(),
            _ => Array.Empty<ProjectStatus>()
        };

        if (!allowed.Contains(target))
        {
            return Error.Validation("NewStatus",
                $"Cannot transition from '{current}' to '{target}'. Allowed: {string.Join(", ", allowed)}.",
                "NOIR-PM-021");
        }

        return null;
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
