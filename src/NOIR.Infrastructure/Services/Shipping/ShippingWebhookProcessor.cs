namespace NOIR.Infrastructure.Services.Shipping;

/// <summary>
/// Processes incoming webhooks from shipping providers.
/// </summary>
public class ShippingWebhookProcessor : IShippingWebhookProcessor, IScopedService
{
    private readonly IShippingProviderFactory _providerFactory;
    private readonly IRepository<ShippingProvider, Guid> _providerRepository;
    private readonly IRepository<ShippingOrder, Guid> _orderRepository;
    private readonly ShippingWebhookLogRepository _webhookLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShippingWebhookProcessor> _logger;

    public ShippingWebhookProcessor(
        IShippingProviderFactory providerFactory,
        IRepository<ShippingProvider, Guid> providerRepository,
        IRepository<ShippingOrder, Guid> orderRepository,
        ShippingWebhookLogRepository webhookLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ShippingWebhookProcessor> logger)
    {
        _providerFactory = providerFactory;
        _providerRepository = providerRepository;
        _orderRepository = orderRepository;
        _webhookLogRepository = webhookLogRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> ProcessWebhookAsync(
        string providerCode,
        string rawPayload,
        string? signature,
        Dictionary<string, string>? headers = null,
        CancellationToken ct = default)
    {
        // Parse provider code
        if (!Enum.TryParse<ShippingProviderCode>(providerCode, true, out var code))
        {
            _logger.LogWarning("Unknown shipping provider code: {ProviderCode}", providerCode);
            return Result.Failure(Error.Validation("providerCode", $"Unknown provider: {providerCode}"));
        }

        // Log the webhook
        var webhookLog = ShippingWebhookLog.Create(
            code,
            $"/api/shipping/webhooks/{providerCode}",
            rawPayload,
            null,
            headers != null ? JsonSerializer.Serialize(headers) : null,
            signature);

        await _webhookLogRepository.AddAsync(webhookLog, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            // Get the provider implementation
            var provider = _providerFactory.GetProvider(code);
            if (provider == null)
            {
                webhookLog.MarkAsFailed($"No implementation for provider: {providerCode}");
                await _unitOfWork.SaveChangesAsync(ct);
                return Result.Failure(Error.Validation("provider", $"No implementation for provider: {providerCode}"));
            }

            // Get provider config for parsing
            var providerSpec = new ShippingProviderByCodeSpec(code);
            var providerConfig = await _providerRepository.FirstOrDefaultAsync(providerSpec, ct);
            if (providerConfig == null)
            {
                webhookLog.MarkAsFailed($"Provider not configured: {providerCode}");
                await _unitOfWork.SaveChangesAsync(ct);
                return Result.Failure(Error.Validation("provider", $"Provider not configured: {providerCode}"));
            }

            // Parse the webhook
            var parseResult = provider.ParseWebhook(rawPayload, signature, providerConfig);
            if (parseResult.IsFailure)
            {
                webhookLog.MarkAsFailed($"Parse failed: {parseResult.Error.Message}");
                await _unitOfWork.SaveChangesAsync(ct);
                return parseResult;
            }

            var payload = parseResult.Value;
            webhookLog.SetTrackingNumber(payload.TrackingNumber);

            // Find the shipping order
            var orderSpec = new ShippingOrderByTrackingNumberForUpdateSpec(payload.TrackingNumber);
            var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, ct);
            if (order == null)
            {
                _logger.LogWarning("Shipping order not found for tracking number: {TrackingNumber}", payload.TrackingNumber);
                webhookLog.MarkAsFailed($"Order not found: {payload.TrackingNumber}");
                await _unitOfWork.SaveChangesAsync(ct);
                return Result.Success(); // Return success to avoid webhook retries
            }

            // Parse the status
            if (!Enum.TryParse<ShippingStatus>(payload.Status, true, out var status))
            {
                _logger.LogWarning("Unknown shipping status: {Status}", payload.Status);
                status = ShippingStatus.InTransit; // Default
            }

            // Create tracking event
            var trackingEvent = ShippingTrackingEvent.Create(
                order.Id,
                payload.EventType,
                status,
                payload.Description,
                payload.Location,
                payload.EventDate,
                payload.RawPayload,
                order.TenantId);

            order.AddTrackingEvent(trackingEvent);

            // Update order status if changed
            if (order.Status != status)
            {
                order.UpdateStatus(status, payload.Location);
            }

            webhookLog.MarkAsProcessed();
            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Processed {Provider} webhook for {TrackingNumber}: {Status}",
                providerCode, payload.TrackingNumber, status);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Provider} webhook", providerCode);
            webhookLog.MarkAsFailed(ex.Message);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result.Failure(Error.Failure(ErrorCodes.External.ServiceUnavailable, $"{providerCode}: {ex.Message}"));
        }
    }
}
