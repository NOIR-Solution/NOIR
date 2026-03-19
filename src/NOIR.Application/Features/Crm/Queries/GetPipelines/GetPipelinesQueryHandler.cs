namespace NOIR.Application.Features.Crm.Queries.GetPipelines;

public class GetPipelinesQueryHandler
{
    private readonly IRepository<Pipeline, Guid> _pipelineRepository;

    public GetPipelinesQueryHandler(IRepository<Pipeline, Guid> pipelineRepository)
    {
        _pipelineRepository = pipelineRepository;
    }

    public async Task<Result<List<Features.Crm.DTOs.PipelineDto>>> Handle(
        GetPipelinesQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.PipelinesListSpec();
        var pipelines = await _pipelineRepository.ListAsync(spec, cancellationToken);

        var items = pipelines.Select(p => new Features.Crm.DTOs.PipelineDto(
            p.Id, p.Name, p.IsDefault,
            p.Stages.OrderBy(s => s.SortOrder)
                .Select(s => new Features.Crm.DTOs.PipelineStageDto(s.Id, s.Name, s.SortOrder, s.Color, s.StageType, s.IsSystem))
                .ToList(),
            p.CreatedAt, p.ModifiedAt)).ToList();

        return Result.Success(items);
    }
}
