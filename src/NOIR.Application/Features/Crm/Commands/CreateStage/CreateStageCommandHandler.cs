namespace NOIR.Application.Features.Crm.Commands.CreateStage;

public class CreateStageCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateStageCommandHandler(
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
        CreateStageCommand command,
        CancellationToken cancellationToken)
    {
        var existingStages = await _dbContext.PipelineStages
            .TagWith("CreateStageCommandHandler")
            .Where(s => s.PipelineId == command.PipelineId)
            .ToListAsync(cancellationToken);

        var systemStages = existingStages.Where(s => s.IsSystem).ToList();
        var lastActiveOrder = existingStages
            .Where(s => !s.IsSystem)
            .Select(s => (int?)s.SortOrder)
            .Max() ?? -1;

        var newSortOrder = lastActiveOrder + 1;

        foreach (var sys in systemStages)
        {
            sys.Update(sys.Name, sys.SortOrder + 1, sys.Color);
        }

        var stage = PipelineStage.Create(
            command.PipelineId,
            command.Name,
            newSortOrder,
            _currentUser.TenantId,
            command.Color);

        _dbContext.PipelineStages.Add(stage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Pipeline",
            entityId: command.PipelineId,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new Features.Crm.DTOs.PipelineStageDto(
            stage.Id, stage.Name, stage.SortOrder, stage.Color, stage.StageType, stage.IsSystem));
    }
}
