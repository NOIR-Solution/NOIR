namespace NOIR.Domain.Entities.Crm;

/// <summary>
/// A stage within a CRM pipeline (e.g., Qualification, Proposal, Negotiation).
/// Won and Lost are system stages that cannot be deleted or renamed.
/// </summary>
public class PipelineStage : TenantEntity<Guid>
{
    public Guid PipelineId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public string Color { get; private set; } = "#6366f1";

    /// <summary>The type of this stage. Active stages are user-configurable; Won/Lost are system stages.</summary>
    public StageType StageType { get; private set; } = StageType.Active;

    /// <summary>System stages (Won/Lost) cannot be deleted, renamed, or reordered past active stages.</summary>
    public bool IsSystem { get; private set; }

    // Navigation properties
    public virtual Pipeline? Pipeline { get; private set; }

    // Private constructor for EF Core
    private PipelineStage() : base() { }

    private PipelineStage(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Creates a user-defined active stage.
    /// </summary>
    public static PipelineStage Create(
        Guid pipelineId,
        string name,
        int sortOrder,
        string? tenantId,
        string color = "#6366f1")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new PipelineStage(Guid.NewGuid(), tenantId)
        {
            PipelineId = pipelineId,
            Name = name.Trim(),
            SortOrder = sortOrder,
            Color = color,
            StageType = StageType.Active,
            IsSystem = false,
        };
    }

    /// <summary>
    /// Creates a system stage (Won or Lost). Always appended at the end.
    /// </summary>
    public static PipelineStage CreateSystem(
        Guid pipelineId,
        StageType type,
        int sortOrder,
        string? tenantId)
    {
        if (type == StageType.Active)
            throw new ArgumentException("Use Create() for active stages.", nameof(type));

        var (name, color) = type == StageType.Won
            ? ("Won", "#22c55e")
            : ("Lost", "#ef4444");

        return new PipelineStage(Guid.NewGuid(), tenantId)
        {
            PipelineId = pipelineId,
            Name = name,
            SortOrder = sortOrder,
            Color = color,
            StageType = type,
            IsSystem = true,
        };
    }

    /// <summary>
    /// Updates stage name, sort order, and color. System stages: only color can change.
    /// </summary>
    public void Update(string name, int sortOrder, string color)
    {
        if (!IsSystem)
            Name = name.Trim();

        SortOrder = sortOrder;
        Color = color;
    }

    /// <summary>
    /// Updates only the color (allowed for both active and system stages).
    /// </summary>
    public void UpdateColor(string color) => Color = color;
}
