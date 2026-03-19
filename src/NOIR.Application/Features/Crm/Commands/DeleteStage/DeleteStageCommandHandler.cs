namespace NOIR.Application.Features.Crm.Commands.DeleteStage;

public class DeleteStageCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<Lead, Guid> _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteStageCommandHandler(
        IApplicationDbContext dbContext,
        IRepository<Lead, Guid> leadRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _dbContext = dbContext;
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.PipelineStageDto>> Handle(
        DeleteStageCommand command,
        CancellationToken cancellationToken)
    {
        var stage = await _dbContext.PipelineStages
            .TagWith("DeleteStageCommandHandler")
            .Where(s => s.Id == command.StageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (stage is null)
        {
            return Result.Failure<Features.Crm.DTOs.PipelineStageDto>(
                Error.NotFound($"Stage with ID '{command.StageId}' not found.", "NOIR-CRM-033"));
        }

        if (stage.IsSystem)
        {
            return Result.Failure<Features.Crm.DTOs.PipelineStageDto>(
                Error.Validation("StageId", "System stages (Won, Lost) cannot be deleted."));
        }

        if (command.StageId == command.MoveLeadsToStageId)
        {
            return Result.Failure<Features.Crm.DTOs.PipelineStageDto>(
                Error.Validation("MoveLeadsToStageId", "Cannot move leads to the stage being deleted."));
        }

        var targetStage = await _dbContext.PipelineStages
            .TagWith("DeleteStageCommandHandler_Target")
            .Where(s => s.Id == command.MoveLeadsToStageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (targetStage is null || targetStage.PipelineId != stage.PipelineId)
        {
            return Result.Failure<Features.Crm.DTOs.PipelineStageDto>(
                Error.Validation("MoveLeadsToStageId", "Target stage not found or belongs to a different pipeline."));
        }

        if (targetStage.IsSystem)
        {
            return Result.Failure<Features.Crm.DTOs.PipelineStageDto>(
                Error.Validation("MoveLeadsToStageId", "Cannot migrate leads to a system stage."));
        }

        // Migrate active leads from deleted stage to target stage
        var leadsSpec = new Specifications.ActiveLeadsByStageTrackingSpec(command.StageId);
        var leads = await _leadRepository.ListAsync(leadsSpec, cancellationToken);

        foreach (var lead in leads)
        {
            lead.MoveToStage(command.MoveLeadsToStageId, lead.SortOrder);
        }

        var dto = new Features.Crm.DTOs.PipelineStageDto(
            stage.Id, stage.Name, stage.SortOrder, stage.Color, stage.StageType, stage.IsSystem);

        _dbContext.PipelineStages.Remove(stage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Pipeline",
            entityId: stage.PipelineId,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(dto);
    }
}
