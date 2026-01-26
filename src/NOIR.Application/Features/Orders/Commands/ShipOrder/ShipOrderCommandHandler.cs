namespace NOIR.Application.Features.Orders.Commands.ShipOrder;

/// <summary>
/// Wolverine handler for shipping an order.
/// </summary>
public class ShipOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ShipOrderCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> Handle(
        ShipOrderCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new OrderByIdForUpdateSpec(command.OrderId);
        var order = await _orderRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderDto>(
                Error.NotFound($"Order with ID '{command.OrderId}' not found.", "NOIR-ORDER-002"));
        }

        // Ensure order is in Processing status first
        if (order.Status == OrderStatus.Confirmed)
        {
            order.StartProcessing();
        }

        try
        {
            order.Ship(command.TrackingNumber, command.ShippingCarrier);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDto>(
                Error.Validation("Status", ex.Message, "NOIR-ORDER-004"));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.ToDto(order));
    }
}
