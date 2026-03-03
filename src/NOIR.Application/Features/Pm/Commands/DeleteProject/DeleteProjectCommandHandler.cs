namespace NOIR.Application.Features.Pm.Commands.DeleteProject;

public class DeleteProjectCommandHandler
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteProjectCommandHandler(
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
        DeleteProjectCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.ProjectByIdSpec(command.Id);
        var project = await _projectRepository.FirstOrDefaultAsync(spec, cancellationToken);
        if (project is null)
        {
            return Result.Failure<Features.Pm.DTOs.ProjectDto>(
                Error.NotFound($"Project with ID '{command.Id}' not found.", "NOIR-PM-002"));
        }

        var dto = MapToDto(project);
        _projectRepository.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Project",
            entityId: command.Id,
            operation: EntityOperation.Deleted,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(dto);
    }

    private static Features.Pm.DTOs.ProjectDto MapToDto(Project p) =>
        new(p.Id, p.Name, p.Slug, p.Description, p.Status,
            p.StartDate, p.EndDate, p.DueDate,
            p.OwnerId, p.Owner != null ? $"{p.Owner.FirstName} {p.Owner.LastName}" : null,
            p.Budget, p.Currency, p.Color, p.Icon, p.Visibility,
            p.Members.Select(m => new Features.Pm.DTOs.ProjectMemberDto(
                m.Id, m.EmployeeId,
                m.Employee != null ? $"{m.Employee.FirstName} {m.Employee.LastName}" : string.Empty,
                m.Employee?.AvatarUrl,
                m.Role, m.JoinedAt)).ToList(),
            p.Columns.OrderBy(c => c.SortOrder).Select(c => new Features.Pm.DTOs.ProjectColumnDto(
                c.Id, c.Name, c.SortOrder, c.Color, c.WipLimit)).ToList(),
            p.CreatedAt, p.ModifiedAt);
}
