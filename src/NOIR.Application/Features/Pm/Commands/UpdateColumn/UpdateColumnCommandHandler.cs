namespace NOIR.Application.Features.Pm.Commands.UpdateColumn;

public class UpdateColumnCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateColumnCommandHandler(
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
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

        return Result.Success(new Features.Pm.DTOs.ProjectColumnDto(
            column.Id, column.Name, column.SortOrder, column.Color, column.WipLimit,
            column.StatusMapping));
    }
}
