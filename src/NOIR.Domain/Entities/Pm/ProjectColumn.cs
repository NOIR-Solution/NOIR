namespace NOIR.Domain.Entities.Pm;

/// <summary>
/// A column on the project Kanban board.
/// </summary>
public class ProjectColumn : TenantEntity<Guid>
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public string? Color { get; private set; }
    public int? WipLimit { get; private set; }
    public string? StatusMapping { get; private set; }

    // Navigation properties
    public virtual Project? Project { get; private set; }
    public virtual ICollection<ProjectTask> Tasks { get; private set; } = new List<ProjectTask>();

    // Private constructor for EF Core
    private ProjectColumn() : base() { }

    private ProjectColumn(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Factory method to create a new project column.
    /// </summary>
    public static ProjectColumn Create(
        Guid projectId,
        string name,
        int sortOrder,
        string? tenantId,
        string? color = null,
        int? wipLimit = null,
        string? statusMapping = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new ProjectColumn(Guid.NewGuid(), tenantId)
        {
            ProjectId = projectId,
            Name = name.Trim(),
            SortOrder = sortOrder,
            Color = color,
            WipLimit = wipLimit,
            StatusMapping = statusMapping
        };
    }

    /// <summary>
    /// Updates column details.
    /// </summary>
    public void Update(string name, int sortOrder, string? color, int? wipLimit, string? statusMapping = null)
    {
        Name = name.Trim();
        SortOrder = sortOrder;
        Color = color;
        WipLimit = wipLimit;
        StatusMapping = statusMapping;
    }
}
