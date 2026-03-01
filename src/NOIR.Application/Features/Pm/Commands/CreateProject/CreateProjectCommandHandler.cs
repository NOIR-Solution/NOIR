namespace NOIR.Application.Features.Pm.Commands.CreateProject;

public class CreateProjectCommandHandler
{
    private readonly IRepository<Project, Guid> _projectRepository;
    private readonly IRepository<Employee, Guid> _employeeRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IProjectCodeGenerator _projectCodeGenerator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateProjectCommandHandler(
        IRepository<Project, Guid> projectRepository,
        IRepository<Employee, Guid> employeeRepository,
        IApplicationDbContext dbContext,
        IProjectCodeGenerator projectCodeGenerator,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _projectRepository = projectRepository;
        _employeeRepository = employeeRepository;
        _dbContext = dbContext;
        _projectCodeGenerator = projectCodeGenerator;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<Features.Pm.DTOs.ProjectDto>> Handle(
        CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Generate slug from name
        var slug = GenerateSlug(command.Name);

        // Check slug uniqueness
        var slugSpec = new Specifications.ProjectBySlugSpec(slug);
        var existingBySlug = await _projectRepository.FirstOrDefaultAsync(slugSpec, cancellationToken);
        if (existingBySlug is not null)
        {
            return Result.Failure<Features.Pm.DTOs.ProjectDto>(
                Error.Conflict($"A project with slug '{slug}' already exists.", "NOIR-PM-001"));
        }

        // Find current user's employee record for owner
        Guid? ownerId = null;
        if (_currentUser.UserId is not null)
        {
            var employeeSpec = new Features.Hr.Specifications.EmployeeByUserIdSpec(_currentUser.UserId);
            var employee = await _employeeRepository.FirstOrDefaultAsync(employeeSpec, cancellationToken);
            ownerId = employee?.Id;
        }

        // Generate unique project code
        var projectCode = await _projectCodeGenerator.GenerateNextAsync(tenantId, cancellationToken);

        var project = Project.Create(
            command.Name,
            slug,
            projectCode,
            tenantId,
            command.Description,
            command.StartDate,
            command.EndDate,
            command.DueDate,
            ownerId,
            command.Budget,
            command.Currency,
            command.Color,
            command.Icon,
            command.Visibility ?? ProjectVisibility.Private);

        await _projectRepository.AddAsync(project, cancellationToken);

        // Add creator as Owner member
        if (ownerId.HasValue)
        {
            var member = ProjectMember.Create(project.Id, ownerId.Value, ProjectMemberRole.Owner, tenantId);
            _dbContext.ProjectMembers.Add(member);
        }

        // Seed default columns
        var defaultColumns = new[] { ("Todo", 0), ("In Progress", 1), ("In Review", 2), ("Done", 3) };
        foreach (var (name, sortOrder) in defaultColumns)
        {
            var column = ProjectColumn.Create(project.Id, name, sortOrder, tenantId);
            _dbContext.ProjectColumns.Add(column);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload to get navigation properties
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

    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
        slug = Regex.Replace(slug, @"-+", "-");
        slug = slug.Trim('-');
        return slug;
    }
}
