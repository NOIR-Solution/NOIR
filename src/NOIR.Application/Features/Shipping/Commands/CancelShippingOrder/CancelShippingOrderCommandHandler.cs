namespace NOIR.Application.Features.Shipping.Commands.CancelShippingOrder;

/// <summary>
/// Handler for cancelling shipping orders.
/// </summary>
public class CancelShippingOrderCommandHandler
{
    private readonly IRepository<ShippingOrder, Guid> _orderRepository;
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;
    private readonly IShippingProviderFactory _providerFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CancelShippingOrderCommandHandler> _logger;

    public CancelShippingOrderCommandHandler(
        IRepository<ShippingOrder, Guid> orderRepository,
        IRepository<ShippingProvider, Guid> providerRepository,
        IShippingProviderFactory providerFactory,
        IUnitOfWork unitOfWork,
        ILogger<CancelShippingOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _providerRepository = providerRepository;
        _providerFactory = providerFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        CancelShippingOrderCommand command,
        CancellationToken cancellationToken)
    {
        // Get the shipping order
        var orderSpec = new ShippingOrderByTrackingNumberForUpdateSpec(command.TrackingNumber);
        var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, cancellationToken);

        if (order == null)
        {
            return Result.Failure(
                Error.NotFound($"Shipping order not found: {command.TrackingNumber}", ErrorCodes.Shipping.ShipmentNotFound));
        }

        // Check if cancellation is allowed
        if (order.Status == ShippingStatus.Delivered ||
            order.Status == ShippingStatus.Cancelled ||
            order.Status == ShippingStatus.Returned)
        {
            return Result.Failure(
                Error.Conflict($"Cannot cancel shipping order in status {order.Status}"));
        }

        // Get provider configuration
        var providerSpec = new ShippingProviderByIdSpec(order.ProviderId);
        var providerConfig = await _providerRepository.FirstOrDefaultAsync(providerSpec, cancellationToken);

        if (providerConfig == null)
        {
            return Result.Failure(
                Error.NotFound("Provider configuration not found", ErrorCodes.Shipping.ProviderNotFound));
        }

        // Get provider implementation
        var provider = _providerFactory.GetProvider(order.ProviderCode);

        // Try to cancel with provider (if submitted)
        if (order.Status != ShippingStatus.Draft && provider != null)
        {
            var cancelResult = await provider.CancelOrderAsync(
                command.TrackingNumber,
                providerConfig,
                cancellationToken);

            if (cancelResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to cancel shipping order {TrackingNumber} with provider: {Error}",
                    command.TrackingNumber, cancelResult.Error.Message);

                // Still mark as cancelled locally if provider fails (provider may have already processed)
            }
        }

        // Cancel locally
        order.Cancel(command.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Cancelled shipping order {TrackingNumber}: {Reason}",
            command.TrackingNumber, command.Reason);

        return Result.Success();
    }
}
