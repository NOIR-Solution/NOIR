namespace NOIR.Application.Features.Pm.Commands.UpdateColumn;

public class UpdateColumnCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateColumnCommandHandler(
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

    public async Task<Result<Features.Pm.DTOs.ProjectColumnDto>> Handle(
        UpdateColumnCommand command,
        CancellationToken cancellationToken)
    {
        var column = await _dbContext.ProjectColumns
            .TagWith("UpdateColumn_Fetch")
            .FirstOrDefaultAsync(c => c.Id == command.ColumnId, cancellationToken);

        if (column is null)
        {
            return Result.Failure<Features.Pm.DTOs.ProjectColumnDto>(
                Error.NotFound($"Column with ID '{command.ColumnId}' not found.", "NOIR-PM-013"));
        }

        column.Update(command.Name, column.SortOrder, command.Color, command.WipLimit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "ProjectColumn",
            entityId: column.Id,
            operation: EntityOperation.Updated,
            tenantId: _currentUser.TenantId!,
            cancellationToken);

        return Result.Success(new Features.Pm.DTOs.ProjectColumnDto(
            column.Id, column.Name, column.SortOrder, column.Color, column.WipLimit,
            column.StatusMapping));
    }
}
