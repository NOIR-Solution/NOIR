namespace NOIR.Application.Features.Orders.Queries.GetOrderById;

/// <summary>
/// Wolverine handler for getting an order by ID.
/// </summary>
public class GetOrderByIdQueryHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;

    public GetOrderByIdQueryHandler(IRepository<Order, Guid> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<OrderDto>> Handle(
        GetOrderByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new OrderByIdSpec(query.OrderId);
        var order = await _orderRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderDto>(
                Error.NotFound($"Order with ID '{query.OrderId}' not found.", "NOIR-ORDER-002"));
        }

        return Result.Success(OrderMapper.ToDto(order));
    }
}
