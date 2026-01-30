namespace NOIR.Application.Features.Shipping.Queries.GetShippingOrder;

/// <summary>
/// Handler for getting shipping order details.
/// </summary>
public class GetShippingOrderQueryHandler
{
    private readonly IRepository<ShippingOrder, Guid> _orderRepository;
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;

    public GetShippingOrderQueryHandler(
        IRepository<ShippingOrder, Guid> orderRepository,
        IRepository<ShippingProvider, Guid> providerRepository)
    {
        _orderRepository = orderRepository;
        _providerRepository = providerRepository;
    }

    public async Task<Result<ShippingOrderDto>> Handle(
        GetShippingOrderQuery query,
        CancellationToken cancellationToken)
    {
        var orderSpec = new ShippingOrderByTrackingNumberSpec(query.TrackingNumber);
        var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, cancellationToken);

        if (order == null)
        {
            return Result.Failure<ShippingOrderDto>(
                Error.NotFound($"Shipping order not found: {query.TrackingNumber}", ErrorCodes.Shipping.ShipmentNotFound));
        }

        var providerName = order.Provider?.ProviderName ?? order.ProviderCode.ToString();
        return Result.Success(order.ToDto(providerName));
    }

    public async Task<Result<ShippingOrderDto>> Handle(
        GetShippingOrderByIdQuery query,
        CancellationToken cancellationToken)
    {
        var orderSpec = new ShippingOrderByIdSpec(query.Id);
        var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, cancellationToken);

        if (order == null)
        {
            return Result.Failure<ShippingOrderDto>(
                Error.NotFound($"Shipping order not found: {query.Id}", ErrorCodes.Shipping.ShipmentNotFound));
        }

        var providerName = order.Provider?.ProviderName ?? order.ProviderCode.ToString();
        return Result.Success(order.ToDto(providerName));
    }

    public async Task<Result<ShippingOrderDto>> Handle(
        GetShippingOrderByOrderIdQuery query,
        CancellationToken cancellationToken)
    {
        var orderSpec = new ShippingOrderByOrderIdSpec(query.OrderId);
        var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, cancellationToken);

        if (order == null)
        {
            return Result.Failure<ShippingOrderDto>(
                Error.NotFound($"Shipping order not found for order: {query.OrderId}", ErrorCodes.Shipping.ShipmentNotFound));
        }

        var providerName = order.Provider?.ProviderName ?? order.ProviderCode.ToString();
        return Result.Success(order.ToDto(providerName));
    }
}
