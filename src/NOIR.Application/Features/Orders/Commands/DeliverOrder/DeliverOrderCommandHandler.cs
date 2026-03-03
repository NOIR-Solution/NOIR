namespace NOIR.Application.Features.Orders.Commands.DeliverOrder;

/// <summary>
/// Wolverine handler for marking an order as delivered.
/// </summary>
public class DeliverOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeliverOrderCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IUnitOfWork unitOfWork,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<OrderDto>> Handle(
        DeliverOrderCommand command,
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
            order.MarkAsDelivered();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDto>(
                Error.Validation("Status", ex.Message, ErrorCodes.Order.InvalidDeliverTransition));
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
