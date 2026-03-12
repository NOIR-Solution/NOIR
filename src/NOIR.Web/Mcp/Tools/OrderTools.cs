using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Orders.Commands.CancelOrder;
using NOIR.Application.Features.Orders.Commands.ConfirmOrder;
using NOIR.Application.Features.Orders.Commands.ShipOrder;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Queries.GetOrderById;
using NOIR.Application.Features.Orders.Queries.GetOrders;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for order management.
/// Provides listing, detail view, and lifecycle operations (confirm, ship, cancel).
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Ecommerce.Orders)]
public sealed class OrderTools(IMessageBus bus, ICurrentUser currentUser)
{
    [McpServerTool(Name = "noir_orders_list", ReadOnly = true, Idempotent = true)]
    [Description("List orders with pagination and filtering. Supports filtering by status, customer email, and date range.")]
    public async Task<PagedResult<OrderSummaryDto>> ListOrders(
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        [Description("Filter by order status: Pending, Confirmed, Processing, Shipped, Delivered, Completed, Cancelled, Returned")] string? status = null,
        [Description("Filter by customer email address")] string? customerEmail = null,
        [Description("Filter orders from this date (ISO 8601)")] string? fromDate = null,
        [Description("Filter orders to this date (ISO 8601)")] string? toDate = null,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var orderStatus = status is not null && Enum.TryParse<OrderStatus>(status, true, out var s) ? s : (OrderStatus?)null;
        var from = fromDate is not null ? DateTimeOffset.Parse(fromDate) : (DateTimeOffset?)null;
        var to = toDate is not null ? DateTimeOffset.Parse(toDate) : (DateTimeOffset?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<OrderSummaryDto>>>(
            new GetOrdersQuery(page, pageSize, orderStatus, customerEmail, from, to), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_orders_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full order details by ID, including line items, addresses, payment info, and status history.")]
    public async Task<OrderDto> GetOrder(
        [Description("The order ID (GUID)")] string orderId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<OrderDto>>(
            new GetOrderByIdQuery(Guid.Parse(orderId)), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_orders_confirm", Destructive = false, Idempotent = true)]
    [Description("Confirm a pending order after payment is received. Changes status from Pending to Confirmed.")]
    public async Task<OrderDto> ConfirmOrder(
        [Description("The order ID (GUID) to confirm")] string orderId,
        CancellationToken ct = default)
    {
        var command = new ConfirmOrderCommand(Guid.Parse(orderId)) { UserId = currentUser.UserId };
        var result = await bus.InvokeAsync<Result<OrderDto>>(command, ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_orders_ship", Destructive = false)]
    [Description("Ship a confirmed order with tracking information. Changes status from Confirmed/Processing to Shipped.")]
    public async Task<OrderDto> ShipOrder(
        [Description("The order ID (GUID) to ship")] string orderId,
        [Description("Shipping tracking number")] string trackingNumber,
        [Description("Shipping carrier name (e.g. 'GHTK', 'GHN', 'VNPost')")] string shippingCarrier,
        CancellationToken ct = default)
    {
        var command = new ShipOrderCommand(Guid.Parse(orderId), trackingNumber, shippingCarrier)
        {
            UserId = currentUser.UserId
        };
        var result = await bus.InvokeAsync<Result<OrderDto>>(command, ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_orders_cancel", Destructive = true)]
    [Description("Cancel an order with an optional reason. Releases reserved inventory back to stock. Cannot cancel already shipped/delivered orders.")]
    public async Task<OrderDto> CancelOrder(
        [Description("The order ID (GUID) to cancel")] string orderId,
        [Description("Reason for cancellation (optional)")] string? reason = null,
        CancellationToken ct = default)
    {
        var command = new CancelOrderCommand(Guid.Parse(orderId), reason) { UserId = currentUser.UserId };
        var result = await bus.InvokeAsync<Result<OrderDto>>(command, ct);
        return result.Unwrap();
    }
}
