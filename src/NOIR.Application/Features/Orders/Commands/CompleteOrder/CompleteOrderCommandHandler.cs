namespace NOIR.Application.Features.Orders.Commands.CompleteOrder;

/// <summary>
/// Wolverine handler for completing an order.
/// </summary>
public class CompleteOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CompleteOrderCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IUnitOfWork unitOfWork,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<OrderDto>> Handle(
        CompleteOrderCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new OrderByIdForUpdateSpec(command.OrderId);
        var order = await _orderRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderDto>(
                Error.NotFound($"Order with ID '{command.OrderId}' not found.", ErrorCodes.Order.NotFound));
        }

        try
        {
            order.Complete();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDto>(
                Error.Validation("Status", ex.Message, ErrorCodes.Order.InvalidCompleteTransition));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Order",
            entityId: order.Id,
            operation: EntityOperation.Updated,
            tenantId: order.TenantId!,
            ct: cancellationToken);

        return Result.Success(OrderMapper.ToDto(order));
    }
}
