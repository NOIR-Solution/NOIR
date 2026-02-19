namespace NOIR.Application.Features.Orders.Commands.ConfirmOrder;

/// <summary>
/// Wolverine handler for confirming an order.
/// </summary>
public class ConfirmOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmOrderCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> Handle(
        ConfirmOrderCommand command,
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
            order.Confirm();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDto>(
                Error.Validation("Status", ex.Message, ErrorCodes.Order.InvalidConfirmTransition));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.ToDto(order));
    }
}
