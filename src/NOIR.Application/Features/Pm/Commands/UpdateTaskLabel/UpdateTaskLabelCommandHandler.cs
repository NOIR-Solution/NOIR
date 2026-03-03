namespace NOIR.Application.Features.Pm.Commands.UpdateTaskLabel;

public class UpdateTaskLabelCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateTaskLabelCommandHandler(
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
        UpdateTaskLabelCommand command,
        CancellationToken cancellationToken)
    {
        var label = await _dbContext.TaskLabels
            .Where(l => l.Id == command.LabelId && l.ProjectId == command.ProjectId)
            .TagWith("UpdateTaskLabel_Fetch")
            .FirstOrDefaultAsync(cancellationToken);

        if (label is null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskLabelDto>(
                Error.NotFound($"Label with ID '{command.LabelId}' not found in project.", "NOIR-PM-010"));
        }

        // Check uniqueness (exclude self)
        var duplicate = await _dbContext.TaskLabels
            .Where(l => l.ProjectId == command.ProjectId && l.Name == command.Name && l.Id != command.LabelId)
            .TagWith("UpdateTaskLabel_UniquenessCheck")
            .AnyAsync(cancellationToken);

        if (duplicate)
        {
            return Result.Failure<Features.Pm.DTOs.TaskLabelDto>(
                Error.Conflict($"A label with name '{command.Name}' already exists in this project.", "NOIR-PM-009"));
        }

        label.Update(command.Name, command.Color);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "TaskLabel",
            entityId: label.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new Features.Pm.DTOs.TaskLabelDto(label.Id, label.Name, label.Color));
    }
}
