namespace NOIR.Application.Features.Customers.Queries.GetCustomerOrders;

/// <summary>
/// Wolverine handler for getting a customer's order history.
/// </summary>
public class GetCustomerOrdersQueryHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IRepository<Order, Guid> _orderRepository;

    public GetCustomerOrdersQueryHandler(
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IRepository<Order, Guid> orderRepository)
    {
        _customerRepository = customerRepository;
        _orderRepository = orderRepository;
    }

    public async Task<Result<PagedResult<OrderSummaryDto>>> Handle(
        GetCustomerOrdersQuery query,
        CancellationToken cancellationToken)
    {
        // Verify customer exists
        var customerExists = await _customerRepository.ExistsAsync(query.CustomerId, cancellationToken);

        if (!customerExists)
        {
            return Result.Failure<PagedResult<OrderSummaryDto>>(
                Error.NotFound($"Customer with ID '{query.CustomerId}' not found.", "NOIR-CUSTOMER-002"));
        }

        var skip = (query.Page - 1) * query.PageSize;

        // Get orders by customer ID
        var countSpec = new OrdersByCustomerIdCountSpec(query.CustomerId);
        var totalCount = await _orderRepository.CountAsync(countSpec, cancellationToken);

        var listSpec = new OrdersByCustomerIdSpec(query.CustomerId, skip, query.PageSize);
        var orders = await _orderRepository.ListAsync(listSpec, cancellationToken);

        var items = orders.Select(OrderMapper.ToSummaryDto).ToList();

        var pageIndex = query.Page - 1;
        return Result.Success(PagedResult<OrderSummaryDto>.Create(
            items,
            totalCount,
            pageIndex,
            query.PageSize));
    }
}
