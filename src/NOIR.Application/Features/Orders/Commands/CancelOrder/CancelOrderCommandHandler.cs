namespace NOIR.Application.Features.Orders.Commands.CancelOrder;

/// <summary>
/// Wolverine handler for cancelling an order.
/// </summary>
public class CancelOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> Handle(
        CancelOrderCommand command,
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
            order.Cancel(command.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDto>(
                Error.Validation("Status", ex.Message, ErrorCodes.Order.InvalidCancelTransition));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.ToDto(order));
    }
}
