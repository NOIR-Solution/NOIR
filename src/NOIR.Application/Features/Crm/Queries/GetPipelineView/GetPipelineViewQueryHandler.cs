namespace NOIR.Application.Features.Crm.Queries.GetPipelineView;

public class GetPipelineViewQueryHandler
{
    private readonly IRepository<Pipeline, Guid> _pipelineRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;

    public GetPipelineViewQueryHandler(
        IRepository<Pipeline, Guid> pipelineRepository,
        IRepository<Lead, Guid> leadRepository)
    {
        _pipelineRepository = pipelineRepository;
        _leadRepository = leadRepository;
    }

    public async Task<Result<Features.Crm.DTOs.PipelineViewDto>> Handle(
        GetPipelineViewQuery query,
        CancellationToken cancellationToken)
    {
        var pipelineSpec = new Specifications.PipelineByIdWithLeadsSpec(query.PipelineId);
        var pipeline = await _pipelineRepository.FirstOrDefaultAsync(pipelineSpec, cancellationToken);

        if (pipeline is null)
        {
            return Result.Failure<Features.Crm.DTOs.PipelineViewDto>(
                Error.NotFound($"Pipeline with ID '{query.PipelineId}' not found.", "NOIR-CRM-030"));
        }

        // Always fetch all leads (active + won + lost) — system stages always shown
        var leadsSpec = new Specifications.LeadsByPipelineSpec(query.PipelineId, includeClosedDeals: true);
        var leads = await _leadRepository.ListAsync(leadsSpec, cancellationToken);

        // Active leads grouped by stage
        var activeLeadsByStage = leads
            .Where(l => l.Status == LeadStatus.Active)
            .GroupBy(l => l.StageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Won/Lost leads for system stages
        var wonLeads = leads.Where(l => l.Status == LeadStatus.Won).OrderBy(l => l.SortOrder).ToList();
        var lostLeads = leads.Where(l => l.Status == LeadStatus.Lost).OrderBy(l => l.SortOrder).ToList();

        // Active stages first (ordered by SortOrder), system stages last
        var activeStages = pipeline.Stages
            .Where(s => s.StageType == StageType.Active)
            .OrderBy(s => s.SortOrder);

        var systemStages = pipeline.Stages
            .Where(s => s.StageType != StageType.Active)
            .OrderBy(s => s.SortOrder);

        var stageDtos = activeStages
            .Select(s =>
            {
                var stageLeads = activeLeadsByStage.TryGetValue(s.Id, out var sl) ? sl : [];
                return MapStage(s, stageLeads);
            })
            .Concat(systemStages.Select(s =>
            {
                var stageLeads = s.StageType == StageType.Won ? wonLeads : lostLeads;
                return MapStage(s, stageLeads);
            }))
            .ToList();

        return Result.Success(new Features.Crm.DTOs.PipelineViewDto(
            pipeline.Id, pipeline.Name, stageDtos));
    }

    private static Features.Crm.DTOs.StageWithLeadsDto MapStage(PipelineStage s, List<Lead> stageLeads)
    {
        var leadCards = stageLeads
            .Select(l => new Features.Crm.DTOs.LeadCardDto(
                l.Id, l.Title, l.Contact?.FullName ?? "", l.Company?.Name,
                l.Value, l.Currency,
                l.Owner != null ? $"{l.Owner.FirstName} {l.Owner.LastName}" : null,
                l.Status, l.SortOrder, l.ExpectedCloseDate, l.CreatedAt))
            .ToList();

        return new Features.Crm.DTOs.StageWithLeadsDto(
            s.Id, s.Name, s.SortOrder, s.Color, leadCards,
            stageLeads.Sum(l => l.Value),
            stageLeads.Count,
            s.StageType,
            s.IsSystem);
    }
}
