using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Queries.GetInventoryReceiptById;
using NOIR.Application.Features.Inventory.Queries.GetInventoryReceipts;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for inventory management.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Ecommerce.Inventory)]
public sealed class InventoryTools(IMessageBus bus)
{
    [McpServerTool(Name = "noir_inventory_receipts_list", ReadOnly = true, Idempotent = true)]
    [Description("List inventory receipts (stock-in/stock-out) with pagination and filtering.")]
    public async Task<PagedResult<InventoryReceiptSummaryDto>> ListReceipts(
        [Description("Filter by type: StockIn, StockOut")] string? type = null,
        [Description("Filter by status: Draft, Confirmed, Cancelled")] string? status = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var receiptType = type is not null && Enum.TryParse<InventoryReceiptType>(type, true, out var t) ? t : (InventoryReceiptType?)null;
        var receiptStatus = status is not null && Enum.TryParse<InventoryReceiptStatus>(status, true, out var s) ? s : (InventoryReceiptStatus?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<InventoryReceiptSummaryDto>>>(
            new GetInventoryReceiptsQuery(page, pageSize, receiptType, receiptStatus), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_inventory_receipts_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full inventory receipt details by ID, including line items with product/variant info and quantities.")]
    public async Task<InventoryReceiptDto> GetReceipt(
        [Description("The inventory receipt ID (GUID)")] string receiptId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<InventoryReceiptDto>>(
            new GetInventoryReceiptByIdQuery(Guid.Parse(receiptId)), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_inventory_dashboard", ReadOnly = true, Idempotent = true)]
    [Description("Get inventory dashboard: total stock value, low-stock alerts, recent receipts, and stock movement trends.")]
    public async Task<InventoryDashboardDto> GetDashboard(
        [Description("Inventory threshold for low-stock alerts (default: 10)")] int lowStockThreshold = 10,
        [Description("Number of recent receipts to include (default: 5)")] int recentReceipts = 5,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<InventoryDashboardDto>>(
            new GetInventoryDashboardQuery(lowStockThreshold, recentReceipts), ct);
        return result.Unwrap();
    }
}
