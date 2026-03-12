using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Customers.DTOs;
using NOIR.Application.Features.Customers.Queries.GetCustomerById;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Queries.GetOrderById;
using NOIR.Application.Features.Products.DTOs;
using NOIR.Application.Features.Products.Queries.GetProductById;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Resources;

/// <summary>
/// MCP resources providing live entity data by ID.
/// Addressable resources allow AI agents to reference specific entities directly.
/// </summary>
[McpServerResourceType]
public sealed class EntityResources(IMessageBus bus)
{
    [McpServerResource(UriTemplate = "noir://orders/{orderId}", Name = "order", MimeType = "application/json")]
    [Description("Fetch live order details by ID. Returns full order data including line items, addresses, payment info, and status history.")]
    public async Task<string> GetOrder(
        [Description("The order ID (GUID)")] string orderId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<OrderDto>>(
            new GetOrderByIdQuery(Guid.Parse(orderId)), ct);
        return JsonSerializer.Serialize(result.Unwrap(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    [McpServerResource(UriTemplate = "noir://products/{productId}", Name = "product", MimeType = "application/json")]
    [Description("Fetch live product details by ID. Returns full product data including variants, pricing, inventory levels, and images.")]
    public async Task<string> GetProduct(
        [Description("The product ID (GUID)")] string productId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<ProductDto>>(
            new GetProductByIdQuery(Guid.Parse(productId)), ct);
        return JsonSerializer.Serialize(result.Unwrap(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    [McpServerResource(UriTemplate = "noir://customers/{customerId}", Name = "customer", MimeType = "application/json")]
    [Description("Fetch live customer profile by ID. Returns full customer data including contact info, addresses, order history summary, and segment/tier.")]
    public async Task<string> GetCustomer(
        [Description("The customer ID (GUID)")] string customerId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<CustomerDto>>(
            new GetCustomerByIdQuery(Guid.Parse(customerId)), ct);
        return JsonSerializer.Serialize(result.Unwrap(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
}
