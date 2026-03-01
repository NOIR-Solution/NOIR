namespace NOIR.Domain.Entities.Pm;

/// <summary>
/// A project in the PM module containing tasks, members, and columns.
/// </summary>
public class Project : TenantAggregateRoot<Guid>
{
    public string ProjectCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProjectStatus Status { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public Guid? OwnerId { get; private set; }
    public decimal? Budget { get; private set; }
    public string? Currency { get; private set; }
    public string? Color { get; private set; }
    public string? Icon { get; private set; }
    public ProjectVisibility Visibility { get; private set; }

    // Navigation properties
    public virtual Employee? Owner { get; private set; }
    public virtual ICollection<ProjectMember> Members { get; private set; } = new List<ProjectMember>();
    public virtual ICollection<ProjectColumn> Columns { get; private set; } = new List<ProjectColumn>();
    public virtual ICollection<ProjectTask> Tasks { get; private set; } = new List<ProjectTask>();

    // Private constructor for EF Core
    private Project() : base() { }

    private Project(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new project.
    /// </summary>
    public static Project Create(
        string name,
        string slug,
        string projectCode,
        string? tenantId,
        string? description = null,
        DateTimeOffset? startDate = null,
        DateTimeOffset? endDate = null,
        DateTimeOffset? dueDate = null,
        Guid? ownerId = null,
        decimal? budget = null,
        string? currency = "VND",
        string? color = null,
        string? icon = null,
        ProjectVisibility visibility = ProjectVisibility.Private)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectCode);

        var project = new Project(Guid.NewGuid(), tenantId)
        {
            ProjectCode = projectCode,
            Name = name.Trim(),
            Slug = slug.Trim(),
            Description = description?.Trim(),
            Status = ProjectStatus.Active,
            StartDate = startDate,
            EndDate = endDate,
            DueDate = dueDate,
            OwnerId = ownerId,
            Budget = budget,
            Currency = currency,
            Color = color,
            Icon = icon,
            Visibility = visibility
        };

        project.AddDomainEvent(new Events.Pm.ProjectCreatedEvent(project.Id));
        return project;
    }

    /// <summary>
    /// Updates project details.
    /// </summary>
    public void Update(
        string name,
        string slug,
        string? description,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        DateTimeOffset? dueDate,
        Guid? ownerId,
        decimal? budget,
        string? currency,
        string? color,
        string? icon,
        ProjectVisibility visibility)
    {
        Name = name.Trim();
        Slug = slug.Trim();
        Description = description?.Trim();
        StartDate = startDate;
        EndDate = endDate;
        DueDate = dueDate;
        OwnerId = ownerId;
        Budget = budget;
        Currency = currency;
        Color = color;
        Icon = icon;
        Visibility = visibility;
    }

    /// <summary>
    /// Changes the project status with state machine validation.
    /// </summary>
    public void ChangeStatus(ProjectStatus newStatus)
    {
        Status = newStatus;
        if (newStatus == ProjectStatus.Completed)
            EndDate = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Archives the project.
    /// </summary>
    public void Archive()
    {
        if (Status == ProjectStatus.Archived)
        {
            throw new InvalidOperationException("Project is already archived.");
        }

        Status = ProjectStatus.Archived;
        AddDomainEvent(new Events.Pm.ProjectArchivedEvent(Id, Name));
    }

    /// <summary>
    /// Marks the project as completed.
    /// </summary>
    public void Complete()
    {
        if (Status != ProjectStatus.Active && Status != ProjectStatus.OnHold)
        {
            throw new InvalidOperationException("Only active or on-hold projects can be completed.");
        }

        Status = ProjectStatus.Completed;
        EndDate = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.Pm.ProjectCompletedEvent(Id, Name));
    }

    /// <summary>
    /// Reactivates a completed, archived, or on-hold project.
    /// </summary>
    public void Reactivate()
    {
        if (Status == ProjectStatus.Active)
        {
            throw new InvalidOperationException("Project is already active.");
        }

        Status = ProjectStatus.Active;
    }
}
