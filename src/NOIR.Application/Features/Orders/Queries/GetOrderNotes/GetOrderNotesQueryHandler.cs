namespace NOIR.Application.Features.Orders.Queries.GetOrderNotes;

/// <summary>
/// Wolverine handler for getting notes for an order.
/// </summary>
public class GetOrderNotesQueryHandler
{
    private readonly IApplicationDbContext _dbContext;

    public GetOrderNotesQueryHandler(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<OrderNoteDto>>> Handle(
        GetOrderNotesQuery query,
        CancellationToken cancellationToken)
    {
        var notes = await _dbContext.OrderNotes
            .TagWith("GetOrderNotes")
            .Where(n => n.OrderId == query.OrderId)
            .OrderByDescending(n => n.CreatedAt)
            .AsNoTracking()
            .Select(n => new OrderNoteDto
            {
                Id = n.Id,
                OrderId = n.OrderId,
                Content = n.Content,
                CreatedByUserId = n.CreatedByUserId,
                CreatedByUserName = n.CreatedByUserName,
                IsInternal = n.IsInternal,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<OrderNoteDto>>(notes);
    }
}
