namespace NOIR.Application.Features.Orders.Queries.GetOrders;

/// <summary>
/// Wolverine handler for getting orders with pagination.
/// </summary>
public class GetOrdersQueryHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;

    public GetOrdersQueryHandler(IRepository<Order, Guid> orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(
        GetOrdersQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // Get total count
        var countSpec = new OrdersCountSpec(
            query.Status,
            query.CustomerEmail,
            query.FromDate,
            query.ToDate);
        var totalCount = await _orderRepository.CountAsync(countSpec, cancellationToken);

        // Get orders
        var listSpec = new OrdersListSpec(
            skip,
            query.PageSize,
            query.Status,
            query.CustomerEmail,
            query.FromDate,
            query.ToDate);
        var orders = await _orderRepository.ListAsync(listSpec, cancellationToken);

        var items = orders.Select(OrderMapper.ToSummaryDto).ToList();

        // PagedResult expects pageIndex (0-based), but query.Page is 1-based
        var pageIndex = query.Page - 1;
        return Result.Success(PagedResult<OrderSummaryDto>.Create(
            items,
            totalCount,
            pageIndex,
            query.PageSize));
    }
}
