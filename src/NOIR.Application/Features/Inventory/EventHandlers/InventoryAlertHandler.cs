namespace NOIR.Application.Features.Inventory.EventHandlers;

/// <summary>
/// Handles inventory domain events by sending stock alerts and admin notifications.
/// </summary>
public class InventoryAlertHandler
{
    private const int LowStockThreshold = 10;

    private readonly INotificationService _notificationService;
    private readonly ILogger<InventoryAlertHandler> _logger;

    public InventoryAlertHandler(
        INotificationService notificationService,
        ILogger<InventoryAlertHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(ProductStockChangedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Checking stock level for product {ProductId}, variant {VariantId}: {OldQty} -> {NewQty}",
            evt.ProductId, evt.ProductVariantId, evt.OldQuantity, evt.NewQuantity);

        if (evt.NewQuantity > LowStockThreshold || evt.NewQuantity >= evt.OldQuantity)
            return;

        try
        {
            await _notificationService.SendToRoleAsync(
                "admin",
                NotificationType.Warning,
                NotificationCategory.System,
                "Low Stock Alert",
                $"Product variant {evt.ProductVariantId} is low on stock ({evt.NewQuantity} units remaining).",
                iconClass: "alert-triangle",
                actionUrl: $"/products/{evt.ProductId}",
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send low stock alert for product {ProductId}", evt.ProductId);
        }
    }

    public async Task Handle(InventoryReceiptConfirmedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending inventory receipt confirmation for {ReceiptNumber}", evt.ReceiptNumber);

        try
        {
            await _notificationService.SendToRoleAsync(
                "admin",
                NotificationType.Success,
                NotificationCategory.Workflow,
                "Inventory Receipt Confirmed",
                $"Inventory receipt {evt.ReceiptNumber} ({evt.Type}) has been confirmed.",
                actionUrl: $"/inventory/{evt.ReceiptId}",
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send inventory receipt notification for {ReceiptNumber}", evt.ReceiptNumber);
        }
    }
}
