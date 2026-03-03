namespace NOIR.Application.Features.Crm.Commands.UpdatePipeline;

public class UpdatePipelineCommandHandler
{
    private readonly IRepository<Pipeline, Guid> _pipelineRepository;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _dbContext;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdatePipelineCommandHandler(
        IRepository<Pipeline, Guid> pipelineRepository,
        IRepository<Lead, Guid> leadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IApplicationDbContext dbContext,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _pipelineRepository = pipelineRepository;
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dbContext = dbContext;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.PipelineDto>> Handle(
        UpdatePipelineCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new Specifications.PipelineByIdSpec(command.Id);
        var pipeline = await _pipelineRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (pipeline is null)
        {
            return Result.Failure<Features.Crm.DTOs.PipelineDto>(
                Error.NotFound($"Pipeline with ID '{command.Id}' not found.", "NOIR-CRM-030"));
        }

        // Handle default flag
        if (command.IsDefault && !pipeline.IsDefault)
        {
            var defaultSpec = new Specifications.DefaultPipelineSpec();
            var currentDefault = await _pipelineRepository.FirstOrDefaultAsync(defaultSpec, cancellationToken);
            if (currentDefault is not null && currentDefault.Id != pipeline.Id)
            {
                currentDefault.SetDefault(false);
            }
        }

        pipeline.Update(command.Name);
        pipeline.SetDefault(command.IsDefault);

        // Handle stages: update existing, add new, remove missing
        var existingStages = pipeline.Stages.ToList();
        var commandStageIds = command.Stages
            .Where(s => s.Id.HasValue)
            .Select(s => s.Id!.Value)
            .ToHashSet();

        // Remove stages not in the command (check for active leads first)
        var stagesToRemove = existingStages.Where(s => !commandStageIds.Contains(s.Id)).ToList();
        foreach (var stage in stagesToRemove)
        {
            var activeLeadsSpec = new Specifications.ActiveLeadsByStageSpec(stage.Id);
            var activeLeads = await _leadRepository.CountAsync(activeLeadsSpec, cancellationToken);
            if (activeLeads > 0)
            {
                return Result.Failure<Features.Crm.DTOs.PipelineDto>(
                    Error.Validation("Stages", $"Cannot remove stage '{stage.Name}' with active leads."));
            }

            pipeline.Stages.Remove(stage);
        }

        // Update existing stages and add new ones
        foreach (var stageDto in command.Stages)
        {
            if (stageDto.Id.HasValue)
            {
                var existing = existingStages.FirstOrDefault(s => s.Id == stageDto.Id.Value);
                existing?.Update(stageDto.Name, stageDto.SortOrder, stageDto.Color);
            }
            else
            {
                var newStage = PipelineStage.Create(
                    pipeline.Id,
                    stageDto.Name,
                    stageDto.SortOrder,
                    _currentUser.TenantId,
                    stageDto.Color);
                pipeline.Stages.Add(newStage);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Pipeline",
            entityId: pipeline.Id,
            operation: EntityOperation.Updated,
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
