namespace NOIR.Application.Features.Crm.Commands.CreatePipeline;

public class CreatePipelineCommandHandler
{
    private readonly IRepository<Pipeline, Guid> _pipelineRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreatePipelineCommandHandler(
        IRepository<Pipeline, Guid> pipelineRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _pipelineRepository = pipelineRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.PipelineDto>> Handle(
        CreatePipelineCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // If setting as default, unset previous default
        if (command.IsDefault)
        {
            var defaultSpec = new Specifications.DefaultPipelineSpec();
            var currentDefault = await _pipelineRepository.FirstOrDefaultAsync(defaultSpec, cancellationToken);
            currentDefault?.SetDefault(false);
        }

        var pipeline = Pipeline.Create(command.Name, tenantId, command.IsDefault);

        // Add stages
        foreach (var stageDto in command.Stages)
        {
            var stage = PipelineStage.Create(
                pipeline.Id,
                stageDto.Name,
                stageDto.SortOrder,
                tenantId,
                stageDto.Color);
            pipeline.Stages.Add(stage);
        }

        await _pipelineRepository.AddAsync(pipeline, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Pipeline",
            entityId: pipeline.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(MapToDto(pipeline));
    }

    private static Features.Crm.DTOs.PipelineDto MapToDto(Pipeline p) =>
        new(p.Id, p.Name, p.IsDefault,
            p.Stages.OrderBy(s => s.SortOrder)
                .Select(s => new Features.Crm.DTOs.PipelineStageDto(s.Id, s.Name, s.SortOrder, s.Color))
                .ToList(),
            p.CreatedAt, p.ModifiedAt);
}
