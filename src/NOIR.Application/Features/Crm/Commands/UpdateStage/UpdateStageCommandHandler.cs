namespace NOIR.Application.Features.Crm.Commands.UpdateStage;

public class UpdateStageCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateStageCommandHandler(
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<Features.Crm.DTOs.PipelineStageDto>> Handle(
        UpdateStageCommand command,
        CancellationToken cancellationToken)
    {
        var stage = await _dbContext.PipelineStages
            .TagWith("UpdateStageCommandHandler")
            .Where(s => s.Id == command.StageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (stage is null)
        {
            return Result.Failure<Features.Crm.DTOs.PipelineStageDto>(
                Error.NotFound($"Stage with ID '{command.StageId}' not found.", "NOIR-CRM-032"));
        }

        // System stages: only color update allowed (Update() guards name for IsSystem)
        stage.Update(command.Name, stage.SortOrder, command.Color);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Pipeline",
            entityId: stage.PipelineId,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new Features.Crm.DTOs.PipelineStageDto(
            stage.Id, stage.Name, stage.SortOrder, stage.Color, stage.StageType, stage.IsSystem));
    }
}
