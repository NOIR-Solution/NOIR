namespace NOIR.Infrastructure.Hubs;

using Microsoft.AspNetCore.SignalR;

/// <summary>
/// SignalR hub for real-time payment status updates.
/// Supports tracking specific payment transactions and tenant-wide updates.
/// </summary>
[Authorize]
public class PaymentHub : Hub<IPaymentClient>
{
    private readonly ILogger<PaymentHub> _logger;

    public PaymentHub(ILogger<PaymentHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects. Adds user to their personal and tenant groups.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their personal group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} connected to PaymentHub with connection {ConnectionId}",
                userId, Context.ConnectionId);
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            // Add user to tenant group for tenant-wide payment updates
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            _logger.LogDebug("Connection {ConnectionId} joined tenant group {TenantId}",
                Context.ConnectionId, tenantId);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
            _logger.LogInformation("User {UserId} disconnected from PaymentHub", userId);
        }

        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
        }

        if (exception != null)
        {
            _logger.LogWarning(exception, "User {UserId} disconnected with error", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows clients to subscribe to updates for a specific payment transaction.
    /// </summary>
    /// <param name="transactionId">The payment transaction ID to track.</param>
    public async Task JoinPaymentGroup(string transactionId)
    {
        if (!string.IsNullOrWhiteSpace(transactionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"payment_{transactionId}");
            _logger.LogDebug("Connection {ConnectionId} joined payment group for transaction {TransactionId}",
                Context.ConnectionId, transactionId);
        }
    }

    /// <summary>
    /// Allows clients to unsubscribe from updates for a specific payment transaction.
    /// </summary>
    /// <param name="transactionId">The payment transaction ID to stop tracking.</param>
    public async Task LeavePaymentGroup(string transactionId)
    {
        if (!string.IsNullOrWhiteSpace(transactionId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"payment_{transactionId}");
            _logger.LogDebug("Connection {ConnectionId} left payment group for transaction {TransactionId}",
                Context.ConnectionId, transactionId);
        }
    }

    /// <summary>
    /// Allows clients to subscribe to updates for a specific order's payments.
    /// </summary>
    /// <param name="orderId">The order ID to track payments for.</param>
    public async Task JoinOrderPaymentsGroup(string orderId)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
            _logger.LogDebug("Connection {ConnectionId} joined order payments group for order {OrderId}",
                Context.ConnectionId, orderId);
        }
    }

    /// <summary>
    /// Allows clients to unsubscribe from updates for a specific order's payments.
    /// </summary>
    /// <param name="orderId">The order ID to stop tracking.</param>
    public async Task LeaveOrderPaymentsGroup(string orderId)
    {
        if (!string.IsNullOrWhiteSpace(orderId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
            _logger.LogDebug("Connection {ConnectionId} left order payments group for order {OrderId}",
                Context.ConnectionId, orderId);
        }
    }

    /// <summary>
    /// Allows admin clients to subscribe to all COD payment updates.
    /// Requires PaymentsManage permission.
    /// </summary>
    public async Task JoinCodUpdatesGroup()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"cod_updates_{tenantId}");
            _logger.LogDebug("Connection {ConnectionId} joined COD updates group for tenant {TenantId}",
                Context.ConnectionId, tenantId);
        }
    }

    /// <summary>
    /// Allows admin clients to unsubscribe from COD payment updates.
    /// </summary>
    public async Task LeaveCodUpdatesGroup()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"cod_updates_{tenantId}");
            _logger.LogDebug("Connection {ConnectionId} left COD updates group for tenant {TenantId}",
                Context.ConnectionId, tenantId);
        }
    }

    /// <summary>
    /// Allows admin clients to subscribe to webhook processing updates.
    /// Useful for monitoring payment gateway health and debugging.
    /// </summary>
    public async Task JoinWebhookUpdatesGroup()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"webhooks_{tenantId}");
            _logger.LogDebug("Connection {ConnectionId} joined webhook updates group for tenant {TenantId}",
                Context.ConnectionId, tenantId);
        }
    }

    /// <summary>
    /// Allows admin clients to unsubscribe from webhook processing updates.
    /// </summary>
    public async Task LeaveWebhookUpdatesGroup()
    {
        var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
        if (!string.IsNullOrEmpty(tenantId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"webhooks_{tenantId}");
            _logger.LogDebug("Connection {ConnectionId} left webhook updates group for tenant {TenantId}",
                Context.ConnectionId, tenantId);
        }
    }
}
