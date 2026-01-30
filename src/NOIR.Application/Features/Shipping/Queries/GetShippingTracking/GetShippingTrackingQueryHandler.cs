namespace NOIR.Application.Features.Shipping.Queries.GetShippingTracking;

/// <summary>
/// Handler for getting shipping tracking information.
/// </summary>
public class GetShippingTrackingQueryHandler
{
    private readonly IRepository<ShippingOrder, Guid> _orderRepository;
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;
    private readonly IShippingProviderFactory _providerFactory;
    private readonly ILogger<GetShippingTrackingQueryHandler> _logger;

    public GetShippingTrackingQueryHandler(
        IRepository<ShippingOrder, Guid> orderRepository,
        IRepository<ShippingProvider, Guid> providerRepository,
        IShippingProviderFactory providerFactory,
        ILogger<GetShippingTrackingQueryHandler> logger)
    {
        _orderRepository = orderRepository;
        _providerRepository = providerRepository;
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<Result<TrackingInfoDto>> Handle(
        GetShippingTrackingQuery query,
        CancellationToken cancellationToken)
    {
        // Get the shipping order with tracking events
        var orderSpec = new ShippingOrderByTrackingNumberSpec(query.TrackingNumber);
        var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, cancellationToken);

        if (order == null)
        {
            return Result.Failure<TrackingInfoDto>(
                Error.NotFound($"Shipping order not found: {query.TrackingNumber}", ErrorCodes.Shipping.ShipmentNotFound));
        }

        // Get provider config for name
        var providerSpec = new ShippingProviderByIdSpec(order.ProviderId);
        var providerConfig = await _providerRepository.FirstOrDefaultAsync(providerSpec, cancellationToken);

        var providerName = providerConfig?.ProviderName ?? order.ProviderCode.ToString();

        // Optionally fetch real-time tracking from provider
        var provider = _providerFactory.GetProvider(order.ProviderCode);
        if (provider != null && providerConfig != null && order.Status != ShippingStatus.Draft)
        {
            try
            {
                var trackingResult = await provider.GetTrackingAsync(
                    query.TrackingNumber,
                    providerConfig,
                    cancellationToken);

                if (trackingResult.IsSuccess)
                {
                    var providerTracking = trackingResult.Value;

                    // Merge local events with provider events
                    var allEvents = order.TrackingEvents
                        .Select(e => new TrackingEventDto(
                            e.EventType,
                            e.Status,
                            e.Description,
                            e.Location,
                            e.EventDate))
                        .ToList();

                    return Result.Success(new TrackingInfoDto(
                        query.TrackingNumber,
                        order.ProviderCode,
                        providerName,
                        providerTracking.CurrentStatus,
                        providerTracking.StatusDescription,
                        providerTracking.CurrentLocation,
                        providerTracking.EstimatedDeliveryDate ?? order.EstimatedDeliveryDate,
                        providerTracking.ActualDeliveryDate ?? order.ActualDeliveryDate,
                        allEvents.OrderByDescending(e => e.EventDate).ToList(),
                        order.TrackingUrl));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch real-time tracking for {TrackingNumber}", query.TrackingNumber);
                // Fall back to local data
            }
        }

        // Return local tracking data
        var events = order.TrackingEvents
            .OrderByDescending(e => e.EventDate)
            .Select(e => new TrackingEventDto(
                e.EventType,
                e.Status,
                e.Description,
                e.Location,
                e.EventDate))
            .ToList();

        var statusDescription = GetStatusDescription(order.Status);

        return Result.Success(new TrackingInfoDto(
            query.TrackingNumber,
            order.ProviderCode,
            providerName,
            order.Status,
            statusDescription,
            null,
            order.EstimatedDeliveryDate,
            order.ActualDeliveryDate,
            events,
            order.TrackingUrl));
    }

    private static string GetStatusDescription(ShippingStatus status) => status switch
    {
        ShippingStatus.Draft => "Đơn hàng đang được tạo",
        ShippingStatus.AwaitingPickup => "Đang chờ lấy hàng",
        ShippingStatus.PickedUp => "Đã lấy hàng",
        ShippingStatus.InTransit => "Đang vận chuyển",
        ShippingStatus.OutForDelivery => "Đang giao hàng",
        ShippingStatus.Delivered => "Đã giao hàng thành công",
        ShippingStatus.DeliveryFailed => "Giao hàng thất bại",
        ShippingStatus.Cancelled => "Đã hủy",
        ShippingStatus.Returning => "Đang hoàn trả",
        ShippingStatus.Returned => "Đã hoàn trả",
        _ => status.ToString()
    };
}
