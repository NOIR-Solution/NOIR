namespace NOIR.Application.Features.Orders.EventHandlers;

/// <summary>
/// Handles order domain events by sending email and in-app notifications.
/// </summary>
public class OrderNotificationHandler
{
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly ILogger<OrderNotificationHandler> _logger;

    public OrderNotificationHandler(
        IEmailService emailService,
        INotificationService notificationService,
        IRepository<Order, Guid> orderRepository,
        IRepository<Customer, Guid> customerRepository,
        ILogger<OrderNotificationHandler> logger)
    {
        _emailService = emailService;
        _notificationService = notificationService;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task Handle(OrderCreatedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending order confirmation for {OrderId} ({OrderNumber})", evt.OrderId, evt.OrderNumber);

        try
        {
            await _emailService.SendTemplateAsync(
                evt.CustomerEmail,
                $"Order Confirmation - {evt.OrderNumber}",
                "order_confirmation",
                new { evt.OrderNumber, evt.GrandTotal, evt.Currency },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send order confirmation email for {OrderNumber}", evt.OrderNumber);
        }

        await SendInAppNotificationAsync(
            evt.OrderId,
            NotificationType.Success,
            "Order Placed",
            $"Your order {evt.OrderNumber} has been placed successfully.",
            ct);
    }

    public async Task Handle(OrderShippedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending shipping notification for {OrderNumber}", evt.OrderNumber);

        var order = await _orderRepository.GetByIdAsync(evt.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for shipping notification", evt.OrderId);
            return;
        }

        try
        {
            await _emailService.SendTemplateAsync(
                order.CustomerEmail,
                $"Order Shipped - {evt.OrderNumber}",
                "order_shipped",
                new { evt.OrderNumber, evt.TrackingNumber, evt.ShippingCarrier },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send shipping email for {OrderNumber}", evt.OrderNumber);
        }

        await SendInAppNotificationAsync(
            order,
            NotificationType.Info,
            "Order Shipped",
            $"Your order {evt.OrderNumber} has been shipped via {evt.ShippingCarrier}. Tracking: {evt.TrackingNumber}",
            ct);
    }

    public async Task Handle(OrderDeliveredEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending delivery confirmation for {OrderNumber}", evt.OrderNumber);

        var order = await _orderRepository.GetByIdAsync(evt.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for delivery notification", evt.OrderId);
            return;
        }

        try
        {
            await _emailService.SendTemplateAsync(
                order.CustomerEmail,
                $"Order Delivered - {evt.OrderNumber}",
                "order_delivered",
                new { evt.OrderNumber },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send delivery email for {OrderNumber}", evt.OrderNumber);
        }

        await SendInAppNotificationAsync(
            order,
            NotificationType.Success,
            "Order Delivered",
            $"Your order {evt.OrderNumber} has been delivered.",
            ct);
    }

    public async Task Handle(OrderCancelledEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending cancellation notification for {OrderNumber}", evt.OrderNumber);

        var order = await _orderRepository.GetByIdAsync(evt.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for cancellation notification", evt.OrderId);
            return;
        }

        try
        {
            await _emailService.SendTemplateAsync(
                order.CustomerEmail,
                $"Order Cancelled - {evt.OrderNumber}",
                "order_cancelled",
                new { evt.OrderNumber, evt.CancellationReason },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send cancellation email for {OrderNumber}", evt.OrderNumber);
        }

        await SendInAppNotificationAsync(
            order,
            NotificationType.Warning,
            "Order Cancelled",
            $"Your order {evt.OrderNumber} has been cancelled.{(evt.CancellationReason is not null ? $" Reason: {evt.CancellationReason}" : "")}",
            ct);
    }

    public async Task Handle(OrderRefundedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending refund confirmation for {OrderNumber}", evt.OrderNumber);

        var order = await _orderRepository.GetByIdAsync(evt.OrderId, ct);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for refund notification", evt.OrderId);
            return;
        }

        try
        {
            await _emailService.SendTemplateAsync(
                order.CustomerEmail,
                $"Refund Processed - {evt.OrderNumber}",
                "order_refunded",
                new { evt.OrderNumber, evt.RefundAmount },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send refund email for {OrderNumber}", evt.OrderNumber);
        }

        await SendInAppNotificationAsync(
            order,
            NotificationType.Info,
            "Refund Processed",
            $"A refund of {evt.RefundAmount} has been processed for order {evt.OrderNumber}.",
            ct);
    }

    #region Private Methods

    private async Task SendInAppNotificationAsync(
        Guid orderId,
        NotificationType type,
        string title,
        string message,
        CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, ct);
        if (order is null) return;

        await SendInAppNotificationAsync(order, type, title, message, ct);
    }

    private async Task SendInAppNotificationAsync(
        Order order,
        NotificationType type,
        string title,
        string message,
        CancellationToken ct)
    {
        if (!order.CustomerId.HasValue) return;

        try
        {
            var customer = await _customerRepository.GetByIdAsync(order.CustomerId.Value, ct);
            if (customer?.UserId is null) return;

            await _notificationService.SendToUserAsync(
                customer.UserId,
                type,
                NotificationCategory.Workflow,
                title,
                message,
                actionUrl: $"/orders/{order.Id}",
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send in-app notification for order {OrderId}", order.Id);
        }
    }

    #endregion
}
