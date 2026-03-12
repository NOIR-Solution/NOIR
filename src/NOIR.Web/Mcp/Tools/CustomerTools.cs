using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Customers.DTOs;
using NOIR.Application.Features.Customers.Queries.GetCustomerById;
using NOIR.Application.Features.Customers.Queries.GetCustomerOrders;
using NOIR.Application.Features.Customers.Queries.GetCustomers;
using NOIR.Application.Features.Customers.Queries.GetCustomerStats;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for customer management.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Ecommerce.Customers)]
public sealed class CustomerTools(IMessageBus bus)
{
    [McpServerTool(Name = "noir_customers_list", ReadOnly = true, Idempotent = true)]
    [Description("List customers with pagination and filtering. Supports search, segment, tier, and active status filters.")]
    public async Task<PagedResult<CustomerSummaryDto>> ListCustomers(
        [Description("Search by name, email, or phone")] string? search = null,
        [Description("Filter by segment: New, Active, AtRisk, Dormant, Lost, VIP")] string? segment = null,
        [Description("Filter by tier: Standard, Silver, Gold, Platinum, Diamond")] string? tier = null,
        [Description("Filter by active status")] bool? isActive = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var seg = segment is not null && Enum.TryParse<CustomerSegment>(segment, true, out var s) ? s : (CustomerSegment?)null;
        var t = tier is not null && Enum.TryParse<CustomerTier>(tier, true, out var tv) ? tv : (CustomerTier?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<CustomerSummaryDto>>>(
            new GetCustomersQuery(page, pageSize, search, seg, t, isActive), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_customers_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full customer details by ID, including addresses, loyalty points, segment, and tier information.")]
    public async Task<CustomerDto> GetCustomer(
        [Description("The customer ID (GUID)")] string customerId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<CustomerDto>>(
            new GetCustomerByIdQuery(Guid.Parse(customerId)), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_customers_orders", ReadOnly = true, Idempotent = true)]
    [Description("Get order history for a specific customer with pagination.")]
    public async Task<PagedResult<OrderSummaryDto>> GetCustomerOrders(
        [Description("The customer ID (GUID)")] string customerId,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await bus.InvokeAsync<Result<PagedResult<OrderSummaryDto>>>(
            new GetCustomerOrdersQuery(Guid.Parse(customerId), page, pageSize), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_customers_stats", ReadOnly = true, Idempotent = true)]
    [Description("Get customer analytics: segment distribution, tier distribution, and top spenders.")]
    public async Task<CustomerStatsDto> GetCustomerStats(
        [Description("Number of top spenders to include (default: 10)")] int topSpendersCount = 10,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<CustomerStatsDto>>(
            new GetCustomerStatsQuery(topSpendersCount), ct);
        return result.Unwrap();
    }
}
