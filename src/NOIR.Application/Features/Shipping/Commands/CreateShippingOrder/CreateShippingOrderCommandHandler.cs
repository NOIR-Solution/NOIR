namespace NOIR.Application.Features.Shipping.Commands.CreateShippingOrder;

/// <summary>
/// Handler for creating shipping orders.
/// </summary>
public class CreateShippingOrderCommandHandler
{
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;
    private readonly IRepository<ShippingOrder, Guid> _orderRepository;
    private readonly IShippingProviderFactory _providerFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CreateShippingOrderCommandHandler> _logger;

    public CreateShippingOrderCommandHandler(
        IRepository<ShippingProvider, Guid> providerRepository,
        IRepository<ShippingOrder, Guid> orderRepository,
        IShippingProviderFactory providerFactory,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ILogger<CreateShippingOrderCommandHandler> logger)
    {
        _providerRepository = providerRepository;
        _orderRepository = orderRepository;
        _providerFactory = providerFactory;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<ShippingOrderDto>> Handle(
        CreateShippingOrderCommand command,
        CancellationToken cancellationToken)
    {
        // Get provider configuration
        var providerSpec = new ShippingProviderByCodeSpec(command.ProviderCode);
        var providerConfig = await _providerRepository.FirstOrDefaultAsync(providerSpec, cancellationToken);

        if (providerConfig == null || !providerConfig.IsActive)
        {
            return Result.Failure<ShippingOrderDto>(
                Error.NotFound($"Shipping provider '{command.ProviderCode}' is not configured or inactive.", ErrorCodes.Shipping.ProviderNotActive));
        }

        // Get provider implementation
        var provider = _providerFactory.GetProvider(command.ProviderCode);
        if (provider == null)
        {
            return Result.Failure<ShippingOrderDto>(
                Error.Failure(ErrorCodes.Shipping.ProviderNotConfigured, $"No implementation available for shipping provider '{command.ProviderCode}'."));
        }

        // Create the domain entity first (in Draft status)
        var shippingOrder = ShippingOrder.Create(
            command.OrderId,
            providerConfig.Id,
            command.ProviderCode,
            command.ServiceTypeCode,
            ShippingProviderMetadata.GetServiceTypeName(command.ProviderCode, command.ServiceTypeCode),
            JsonSerializer.Serialize(command.PickupAddress),
            JsonSerializer.Serialize(command.DeliveryAddress),
            JsonSerializer.Serialize(command.Sender),
            JsonSerializer.Serialize(command.Recipient),
            JsonSerializer.Serialize(command.Items),
            command.TotalWeightGrams,
            command.DeclaredValue,
            command.CodAmount,
            command.IsFreeship,
            command.Notes,
            _currentUser.TenantId);

        await _orderRepository.AddAsync(shippingOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create request for provider
        var createRequest = new Features.Shipping.DTOs.CreateShippingOrderRequest(
            command.OrderId,
            command.ProviderCode,
            command.ServiceTypeCode,
            command.PickupAddress,
            command.DeliveryAddress,
            command.Sender,
            command.Recipient,
            command.Items,
            command.TotalWeightGrams,
            command.DeclaredValue,
            command.CodAmount,
            command.IsFreeship,
            command.RequireInsurance,
            command.Notes,
            null);

        // Submit to provider
        var result = await provider.CreateOrderAsync(createRequest, providerConfig, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError("Failed to create shipping order with {Provider}: {Error}",
                command.ProviderCode, result.Error.Message);

            // Cancel the draft order
            shippingOrder.Cancel($"Provider rejected: {result.Error.Message}");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<ShippingOrderDto>(result.Error);
        }

        var providerResult = result.Value;

        // Update with provider response
        shippingOrder.SetProviderResponse(
            providerResult.TrackingNumber,
            providerResult.ProviderOrderId,
            providerResult.LabelUrl,
            providerConfig.GetTrackingUrl(providerResult.TrackingNumber),
            providerResult.ShippingFee,
            providerResult.CodFee ?? 0,
            providerResult.InsuranceFee ?? 0,
            providerResult.EstimatedDeliveryDate,
            providerResult.RawResponse);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created shipping order {TrackingNumber} for order {OrderId} via {Provider}",
            providerResult.TrackingNumber, command.OrderId, command.ProviderCode);

        return Result.Success(shippingOrder.ToDto(providerConfig.ProviderName));
    }

}
