namespace NOIR.Application.Features.Orders.Commands.DeleteOrderNote;

/// <summary>
/// Wolverine handler for deleting an order note.
/// </summary>
public class DeleteOrderNoteCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteOrderNoteCommandHandler(
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<OrderNoteDto>> Handle(
        DeleteOrderNoteCommand command,
        CancellationToken cancellationToken)
    {
        var note = await _dbContext.OrderNotes
            .TagWith("DeleteOrderNote")
            .FirstOrDefaultAsync(
                n => n.Id == command.NoteId && n.OrderId == command.OrderId,
                cancellationToken);

        if (note is null)
        {
            return Result.Failure<OrderNoteDto>(
                Error.NotFound($"Order note with ID '{command.NoteId}' not found.", ErrorCodes.Order.NoteNotFound));
        }

        _dbContext.OrderNotes.Remove(note);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Order",
            entityId: command.OrderId,
            operation: EntityOperation.Updated,
            tenantId: note.TenantId!,
            ct: cancellationToken);

        return Result.Success(OrderMapper.ToDto(note));
    }
}
