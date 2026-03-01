namespace NOIR.Application.Features.Pm.Commands.ReorderColumns;

public class ReorderColumnsCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public ReorderColumnsCommandHandler(
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<Features.Pm.DTOs.ProjectColumnDto>>> Handle(
        ReorderColumnsCommand command,
        CancellationToken cancellationToken)
    {
        var columns = await _dbContext.ProjectColumns
            .Where(c => c.ProjectId == command.ProjectId)
            .OrderBy(c => c.SortOrder)
            .TagWith("ReorderColumns_Fetch")
            .ToListAsync(cancellationToken);

        var columnLookup = columns.ToDictionary(c => c.Id);

        // Reorder by ColumnIds list index
        for (var i = 0; i < command.ColumnIds.Count; i++)
        {
            if (columnLookup.TryGetValue(command.ColumnIds[i], out var column))
            {
                column.Update(column.Name, i, column.Color, column.WipLimit);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload
        var reloaded = await _dbContext.ProjectColumns
            .Where(c => c.ProjectId == command.ProjectId)
            .OrderBy(c => c.SortOrder)
            .TagWith("ReorderColumns_Reload")
            .ToListAsync(cancellationToken);

        return Result.Success(reloaded.Select(c => new Features.Pm.DTOs.ProjectColumnDto(
            c.Id, c.Name, c.SortOrder, c.Color, c.WipLimit,
            c.StatusMapping)).ToList());
    }
}
