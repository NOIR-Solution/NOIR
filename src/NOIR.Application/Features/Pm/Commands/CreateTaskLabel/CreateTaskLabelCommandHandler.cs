namespace NOIR.Application.Features.Pm.Commands.CreateTaskLabel;

public class CreateTaskLabelCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateTaskLabelCommandHandler(
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
        CreateTaskLabelCommand command,
        CancellationToken cancellationToken)
    {
        // Check uniqueness within project
        var existing = await _dbContext.TaskLabels
            .Where(l => l.ProjectId == command.ProjectId && l.Name == command.Name)
            .TagWith("CreateTaskLabel_UniquenessCheck")
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<Features.Pm.DTOs.TaskLabelDto>(
                Error.Conflict($"A label with name '{command.Name}' already exists in this project.", "NOIR-PM-009"));
        }

        var label = TaskLabel.Create(command.ProjectId, command.Name, command.Color, _currentUser.TenantId);
        _dbContext.TaskLabels.Add(label);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "TaskLabel",
            entityId: label.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new Features.Pm.DTOs.TaskLabelDto(label.Id, label.Name, label.Color));
    }
}
