namespace NOIR.Application.Features.Pm.Commands.DeleteTaskLabel;

public class DeleteTaskLabelCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteTaskLabelCommandHandler(
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

    public async Task<Result<Features.Pm.DTOs.TaskLabelDto>> Handle(
        DeleteTaskLabelCommand command,
        CancellationToken cancellationToken)
    {
        var label = await _dbContext.TaskLabels
            .TagWith("DeleteTaskLabel_Fetch")
            .FirstOrDefaultAsync(l => l.Id == command.LabelId, cancellationToken);

        if (label is null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskLabelDto>(
                Error.NotFound($"Label with ID '{command.LabelId}' not found.", "NOIR-PM-010"));
        }

        var dto = new Features.Pm.DTOs.TaskLabelDto(label.Id, label.Name, label.Color);
        _dbContext.TaskLabels.Remove(label);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "TaskLabel",
            entityId: command.LabelId,
            operation: EntityOperation.Deleted,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(dto);
    }
}
