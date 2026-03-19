namespace NOIR.Application.Features.Crm.Specifications;

/// <summary>
/// Get lead by ID with tracking for mutations.
/// </summary>
public sealed class LeadByIdSpec : Specification<Lead>
{
    public LeadByIdSpec(Guid id)
    {
        Query.Where(l => l.Id == id)
             .Include(l => l.Contact!)
             .Include(l => l.Company!)
             .Include(l => l.Owner!)
             .Include(l => l.Pipeline!)
             .Include(l => l.Stage!)
             .AsTracking()
             .TagWith("LeadById");
    }
}

/// <summary>
/// Get lead by ID read-only (for queries).
/// </summary>
public sealed class LeadByIdReadOnlySpec : Specification<Lead>
{
    public LeadByIdReadOnlySpec(Guid id)
    {
        Query.Where(l => l.Id == id)
             .Include(l => l.Contact!)
             .Include(l => l.Company!)
             .Include(l => l.Owner!)
             .Include(l => l.Pipeline!)
             .Include(l => l.Stage!)
             .TagWith("LeadByIdReadOnly");
    }
}

/// <summary>
/// Paginated, filterable lead list.
/// </summary>
public sealed class LeadsFilterSpec : Specification<Lead>
{
    public LeadsFilterSpec(
        Guid? pipelineId = null,
        Guid? stageId = null,
        Guid? ownerId = null,
        LeadStatus? status = null,
        int? skip = null,
        int? take = null)
    {
        if (pipelineId.HasValue)
            Query.Where(l => l.PipelineId == pipelineId.Value);

        if (stageId.HasValue)
            Query.Where(l => l.StageId == stageId.Value);

        if (ownerId.HasValue)
            Query.Where(l => l.OwnerId == ownerId.Value);

        if (status.HasValue)
            Query.Where(l => l.Status == status.Value);

        Query.Include(l => l.Contact!)
             .Include(l => l.Owner!)
             .Include(l => l.Stage!)
             .OrderByDescending(l => l.CreatedAt);

        if (skip.HasValue) Query.Skip(skip.Value);
        if (take.HasValue) Query.Take(take.Value);

        Query.TagWith("LeadsFilter");
    }
}

/// <summary>
/// Count leads matching filters (without pagination).
/// </summary>
public sealed class LeadsCountSpec : Specification<Lead>
{
    public LeadsCountSpec(
        Guid? pipelineId = null,
        Guid? stageId = null,
        Guid? ownerId = null,
        LeadStatus? status = null)
    {
        if (pipelineId.HasValue)
            Query.Where(l => l.PipelineId == pipelineId.Value);

        if (stageId.HasValue)
            Query.Where(l => l.StageId == stageId.Value);

        if (ownerId.HasValue)
            Query.Where(l => l.OwnerId == ownerId.Value);

        if (status.HasValue)
            Query.Where(l => l.Status == status.Value);

        Query.TagWith("LeadsCount");
    }
}

/// <summary>
/// Get leads by pipeline for Kanban view.
/// </summary>
public sealed class LeadsByPipelineSpec : Specification<Lead>
{
    public LeadsByPipelineSpec(Guid pipelineId, bool includeClosedDeals = false)
    {
        Query.Where(l => l.PipelineId == pipelineId);

        if (!includeClosedDeals)
            Query.Where(l => l.Status == LeadStatus.Active);

        Query.Include(l => l.Contact!)
             .Include(l => l.Company!)
             .Include(l => l.Owner!)
             .Include(l => l.Stage!)
             .OrderBy(l => l.Stage!.SortOrder)
             .ThenBy(l => l.SortOrder)
             .TagWith("LeadsByPipeline");
    }
}

/// <summary>
/// Count active leads in a pipeline (for delete guard).
/// </summary>
public sealed class ActiveLeadsByPipelineSpec : Specification<Lead>
{
    public ActiveLeadsByPipelineSpec(Guid pipelineId)
    {
        Query.Where(l => l.PipelineId == pipelineId && l.Status == LeadStatus.Active)
             .TagWith("ActiveLeadsByPipeline");
    }
}

/// <summary>
/// Count active leads in a stage (for stage delete guard).
/// </summary>
public sealed class ActiveLeadsByStageSpec : Specification<Lead>
{
    public ActiveLeadsByStageSpec(Guid stageId)
    {
        Query.Where(l => l.StageId == stageId && l.Status == LeadStatus.Active)
             .TagWith("ActiveLeadsByStage");
    }
}

/// <summary>
/// Get active leads in a stage with tracking (for stage migration on delete).
/// </summary>
public sealed class ActiveLeadsByStageTrackingSpec : Specification<Lead>
{
    public ActiveLeadsByStageTrackingSpec(Guid stageId)
    {
        Query.Where(l => l.StageId == stageId && l.Status == LeadStatus.Active)
             .AsTracking()
             .TagWith("ActiveLeadsByStageTracking");
    }
}
