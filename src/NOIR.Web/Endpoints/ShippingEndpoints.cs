using NOIR.Application.Features.Shipping.Commands.CreateShippingOrder;
using NOIR.Application.Features.Shipping.Commands.CancelShippingOrder;
using NOIR.Application.Features.Shipping.Queries.CalculateShippingRates;
using NOIR.Application.Features.Shipping.Queries.GetShippingOrder;
using NOIR.Application.Features.Shipping.Queries.GetShippingTracking;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Shipping operation endpoints (rates, orders, tracking, webhooks).
/// </summary>
public static class ShippingEndpoints
{
    public static void MapShippingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/shipping")
            .WithTags("Shipping")
            .RequireAuthorization();

        // ============ Rate Calculation ============

        // Calculate shipping rates from all providers
        group.MapPost("/rates/calculate", async (
            CalculateShippingRatesQuery query,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<ShippingRatesResponse>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("CalculateShippingRates")
        .WithSummary("Calculate shipping rates")
        .WithDescription("Calculates shipping rates from all active providers for the given route and package.")
        .Produces<ShippingRatesResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // ============ Shipping Orders ============

        // Create shipping order
        group.MapPost("/orders", async (
            CreateShippingOrderRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateShippingOrderCommand(
                request.OrderId,
                request.ProviderCode,
                request.ServiceTypeCode,
                request.PickupAddress,
                request.DeliveryAddress,
                request.Sender,
                request.Recipient,
                request.Items,
                request.TotalWeightGrams,
                request.DeclaredValue,
                request.CodAmount,
                request.IsFreeship,
                request.RequireInsurance,
                request.Notes)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<ShippingOrderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("CreateShippingOrder")
        .WithSummary("Create a shipping order")
        .WithDescription("Creates a shipping order with the specified provider and submits it for pickup.")
        .Produces<ShippingOrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get shipping order by tracking number
        group.MapGet("/orders/{trackingNumber}", async (
            string trackingNumber,
            IMessageBus bus) =>
        {
            var query = new GetShippingOrderQuery(trackingNumber);
            var result = await bus.InvokeAsync<Result<ShippingOrderDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersRead)
        .WithName("GetShippingOrder")
        .WithSummary("Get shipping order by tracking number")
        .WithDescription("Returns shipping order details for the given tracking number.")
        .Produces<ShippingOrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get shipping order by NOIR order ID
        group.MapGet("/orders/by-order/{orderId:guid}", async (
            Guid orderId,
            IMessageBus bus) =>
        {
            var query = new GetShippingOrderByOrderIdQuery(orderId);
            var result = await bus.InvokeAsync<Result<ShippingOrderDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersRead)
        .WithName("GetShippingOrderByOrderId")
        .WithSummary("Get shipping order by NOIR order ID")
        .WithDescription("Returns shipping order details for the given NOIR order ID.")
        .Produces<ShippingOrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Cancel shipping order
        group.MapDelete("/orders/{trackingNumber}", async (
            string trackingNumber,
            [FromQuery] string? reason,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CancelShippingOrderCommand(trackingNumber, reason)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("CancelShippingOrder")
        .WithSummary("Cancel a shipping order")
        .WithDescription("Cancels a shipping order if it hasn't been delivered yet.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ============ Tracking ============

        // Get tracking information
        group.MapGet("/tracking/{trackingNumber}", async (
            string trackingNumber,
            IMessageBus bus) =>
        {
            var query = new GetShippingTrackingQuery(trackingNumber);
            var result = await bus.InvokeAsync<Result<TrackingInfoDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersRead)
        .WithName("GetShippingTracking")
        .WithSummary("Get tracking information")
        .WithDescription("Returns tracking information and history for the given tracking number.")
        .Produces<TrackingInfoDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // ============ Webhooks (Public) ============
        MapWebhookEndpoints(app);
    }

    private static void MapWebhookEndpoints(IEndpointRouteBuilder app)
    {
        // Webhook endpoint - must be public (no auth required)
        app.MapPost("/api/shipping/webhooks/{providerCode}", async (
            string providerCode,
            HttpContext context,
            [FromServices] IShippingWebhookProcessor processor) =>
        {
            // Read raw body
            using var reader = new StreamReader(context.Request.Body);
            var rawPayload = await reader.ReadToEndAsync();

            // Get signature from headers (provider-specific)
            var signature = GetWebhookSignature(providerCode, context.Request.Headers);

            // Get relevant headers
            var headers = context.Request.Headers
                .Where(h => !h.Key.StartsWith(":", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(h => h.Key, h => h.Value.ToString());

            var result = await processor.ProcessWebhookAsync(providerCode, rawPayload, signature, headers);

            // Always return 200 to prevent webhook retries
            return Results.Ok(new { success = result.IsSuccess, message = result.IsSuccess ? "OK" : result.Error.Message });
        })
        .AllowAnonymous()
        .WithTags("Shipping Webhooks")
        .WithName("ProcessShippingWebhook")
        .WithSummary("Process shipping provider webhook")
        .WithDescription("Receives and processes webhook notifications from shipping providers.")
        .Produces<object>(StatusCodes.Status200OK);
    }

    private static string? GetWebhookSignature(string providerCode, IHeaderDictionary headers)
    {
        return providerCode.ToUpperInvariant() switch
        {
            "GHTK" => headers.TryGetValue("X-GHTK-Signature", out var ghtkSig) ? ghtkSig.ToString() : null,
            "GHN" => headers.TryGetValue("X-GHN-Signature", out var ghnSig) ? ghnSig.ToString() : null,
            _ => headers.TryGetValue("X-Signature", out var sig) ? sig.ToString() : null
        };
    }
}
