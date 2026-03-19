namespace NOIR.Application.Features.Crm.Specifications;

/// <summary>
/// Get pipeline by ID with tracking for mutations.
/// </summary>
public sealed class PipelineByIdSpec : Specification<Pipeline>
{
    public PipelineByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .Include(p => p.Stages.OrderBy(s => s.SortOrder))
             .AsTracking()
             .TagWith("PipelineById");
    }
}

/// <summary>
/// Get pipeline by ID read-only (for queries).
/// </summary>
public sealed class PipelineByIdReadOnlySpec : Specification<Pipeline>
{
    public PipelineByIdReadOnlySpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .Include(p => p.Stages.OrderBy(s => s.SortOrder))
             .TagWith("PipelineByIdReadOnly");
    }
}

/// <summary>
/// Get pipeline with leads for Kanban view (split query to avoid cartesian explosion).
/// </summary>
public sealed class PipelineByIdWithLeadsSpec : Specification<Pipeline>
{
    public PipelineByIdWithLeadsSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .Include(p => p.Stages.OrderBy(s => s.SortOrder))
             .AsSplitQuery()
             .TagWith("PipelineByIdWithLeads");
    }
}

/// <summary>
/// Get all pipelines with stages.
/// </summary>
public sealed class PipelinesListSpec : Specification<Pipeline>
{
    public PipelinesListSpec()
    {
        Query.Include(p => p.Stages.OrderBy(s => s.SortOrder))
             .OrderByDescending(p => p.IsDefault)
             .ThenBy(p => p.Name)
             .TagWith("PipelinesList");
    }
}

/// <summary>
/// Get the default pipeline.
/// </summary>
public sealed class DefaultPipelineSpec : Specification<Pipeline>
{
    public DefaultPipelineSpec()
    {
        Query.Where(p => p.IsDefault)
             .AsTracking()
             .TagWith("DefaultPipeline");
    }
}

/// <summary>
/// Get a single pipeline stage by ID (read-only).
/// </summary>
public sealed class StageByIdSpec : Specification<PipelineStage>
{
    public StageByIdSpec(Guid id)
    {
        Query.Where(s => s.Id == id)
             .TagWith("StageById");
    }
}

/// <summary>
/// Get a single pipeline stage by ID with tracking for mutations.
/// </summary>
public sealed class StageByIdTrackingSpec : Specification<PipelineStage>
{
    public StageByIdTrackingSpec(Guid id)
    {
        Query.Where(s => s.Id == id)
             .AsTracking()
             .TagWith("StageByIdTracking");
    }
}

/// <summary>
/// Get all stages for a pipeline ordered by sort order.
/// </summary>
public sealed class StagesByPipelineSpec : Specification<PipelineStage>
{
    public StagesByPipelineSpec(Guid pipelineId, bool tracking = false)
    {
        Query.Where(s => s.PipelineId == pipelineId)
             .OrderBy(s => s.SortOrder)
             .TagWith("StagesByPipeline");

        if (tracking) Query.AsTracking();
    }
}
