namespace NOIR.Application.Features.Crm.Commands.ReorderStages;

public class ReorderStagesCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public ReorderStagesCommandHandler(
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

    public async Task<Result<List<Features.Crm.DTOs.PipelineStageDto>>> Handle(
        ReorderStagesCommand command,
        CancellationToken cancellationToken)
    {
        var allStages = await _dbContext.PipelineStages
            .TagWith("ReorderStagesCommandHandler")
            .Where(s => s.PipelineId == command.PipelineId)
            .ToListAsync(cancellationToken);

        var systemStageIds = allStages.Where(s => s.IsSystem).Select(s => s.Id).ToHashSet();
        var invalidIds = command.StageIds.Where(id => systemStageIds.Contains(id)).ToList();

        if (invalidIds.Count > 0)
        {
            return Result.Failure<List<Features.Crm.DTOs.PipelineStageDto>>(
                Error.Validation("StageIds", "System stages (Won, Lost) cannot be reordered."));
        }

        var pipelineActiveIds = allStages.Where(s => !s.IsSystem).Select(s => s.Id).ToHashSet();
        var unknownIds = command.StageIds.Where(id => !pipelineActiveIds.Contains(id)).ToList();

        if (unknownIds.Count > 0)
        {
            return Result.Failure<List<Features.Crm.DTOs.PipelineStageDto>>(
                Error.Validation("StageIds", "One or more stage IDs do not belong to this pipeline."));
        }

        for (var i = 0; i < command.StageIds.Count; i++)
        {
            var stage = allStages.First(s => s.Id == command.StageIds[i]);
            stage.Update(stage.Name, i, stage.Color);
        }

        var systemBaseOrder = command.StageIds.Count;
        foreach (var sysStage in allStages.Where(s => s.IsSystem).OrderBy(s => s.SortOrder))
        {
            sysStage.Update(sysStage.Name, systemBaseOrder++, sysStage.Color);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Pipeline",
            entityId: command.PipelineId,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(allStages
            .OrderBy(s => s.SortOrder)
            .Select(s => new Features.Crm.DTOs.PipelineStageDto(s.Id, s.Name, s.SortOrder, s.Color, s.StageType, s.IsSystem))
            .ToList());
    }
}
