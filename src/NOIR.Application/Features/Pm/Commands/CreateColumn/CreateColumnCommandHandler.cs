namespace NOIR.Application.Features.Pm.Commands.CreateColumn;

public class CreateColumnCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateColumnCommandHandler(
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<Features.Pm.DTOs.ProjectColumnDto>> Handle(
        CreateColumnCommand command,
        CancellationToken cancellationToken)
    {
        // Determine next sort order
        var existingColumns = await _dbContext.ProjectColumns
            .Where(c => c.ProjectId == command.ProjectId)
            .OrderBy(c => c.SortOrder)
            .TagWith("CreateColumn_ExistingColumns")
            .ToListAsync(cancellationToken);

        var nextSortOrder = existingColumns.Count > 0 ? existingColumns.Max(c => c.SortOrder) + 1 : 0;

        var column = ProjectColumn.Create(
            command.ProjectId, command.Name, nextSortOrder, _currentUser.TenantId,
            command.Color, command.WipLimit);

        _dbContext.ProjectColumns.Add(column);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new Features.Pm.DTOs.ProjectColumnDto(
            column.Id, column.Name, column.SortOrder, column.Color, column.WipLimit,
            column.StatusMapping));
    }
}
