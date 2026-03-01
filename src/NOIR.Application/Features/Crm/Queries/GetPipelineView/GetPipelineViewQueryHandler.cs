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

        // Get leads for this pipeline
        var leadsSpec = new Specifications.LeadsByPipelineSpec(query.PipelineId, query.IncludeClosedDeals);
        var leads = await _leadRepository.ListAsync(leadsSpec, cancellationToken);

        // Group leads by stage
        var leadsByStage = leads.GroupBy(l => l.StageId).ToDictionary(g => g.Key, g => g.ToList());

        var stages = pipeline.Stages
            .OrderBy(s => s.SortOrder)
            .Select(s =>
            {
                var stageLeads = leadsByStage.TryGetValue(s.Id, out var sl) ? sl : [];
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
                    stageLeads.Count);
            })
            .ToList();

        return Result.Success(new Features.Crm.DTOs.PipelineViewDto(
            pipeline.Id, pipeline.Name, stages));
    }
}
