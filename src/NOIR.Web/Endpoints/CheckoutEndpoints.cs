using NOIR.Application.Features.Checkout.Commands.CompleteCheckout;
using NOIR.Application.Features.Checkout.Commands.InitiateCheckout;
using NOIR.Application.Features.Checkout.Commands.SelectPaymentMethod;
using NOIR.Application.Features.Checkout.Commands.SelectShippingMethod;
using NOIR.Application.Features.Checkout.Commands.SetCheckoutAddress;
using NOIR.Application.Features.Checkout.DTOs;
using NOIR.Application.Features.Checkout.Queries.GetCheckoutSession;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Checkout API endpoints.
/// Provides checkout flow operations from cart to order.
/// </summary>
public static class CheckoutEndpoints
{
    public static void MapCheckoutEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/checkout")
            .WithTags("Checkout")
            .RequireFeature(ModuleNames.Ecommerce.Checkout)
            .RequireAuthorization();

        // Get checkout session
        group.MapGet("/{sessionId:guid}", async (Guid sessionId, IMessageBus bus) =>
        {
            var query = new GetCheckoutSessionQuery(sessionId);
            var result = await bus.InvokeAsync<Result<CheckoutSessionDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersRead)
        .WithName("GetCheckoutSession")
        .WithSummary("Get checkout session by ID")
        .WithDescription("Returns the current state of a checkout session.")
        .Produces<CheckoutSessionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Initiate checkout
        group.MapPost("/initiate", async (
            InitiateCheckoutRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new InitiateCheckoutCommand(
                request.CartId,
                request.CustomerEmail,
                request.CustomerName,
                request.CustomerPhone)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<CheckoutSessionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("InitiateCheckout")
        .WithSummary("Initiate checkout from cart")
        .WithDescription("Creates a new checkout session from an active cart.")
        .Produces<CheckoutSessionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Set shipping address
        group.MapPost("/{sessionId:guid}/shipping-address", async (
            Guid sessionId,
            SetCheckoutAddressRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new SetCheckoutAddressCommand(
                sessionId,
                "Shipping",
                request.FullName,
                request.Phone,
                request.AddressLine1,
                request.AddressLine2,
                request.Ward,
                request.District,
                request.Province,
                request.PostalCode,
                request.Country,
                true)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<CheckoutSessionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("SetCheckoutShippingAddress")
        .WithSummary("Set shipping address")
        .WithDescription("Sets the shipping address for the checkout session.")
        .Produces<CheckoutSessionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Set billing address
        group.MapPost("/{sessionId:guid}/billing-address", async (
            Guid sessionId,
            SetCheckoutAddressRequest request,
            [FromQuery] bool sameAsShipping,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new SetCheckoutAddressCommand(
                sessionId,
                "Billing",
                request.FullName,
                request.Phone,
                request.AddressLine1,
                request.AddressLine2,
                request.Ward,
                request.District,
                request.Province,
                request.PostalCode,
                request.Country,
                sameAsShipping)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<CheckoutSessionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("SetCheckoutBillingAddress")
        .WithSummary("Set billing address")
        .WithDescription("Sets the billing address for the checkout session. Use sameAsShipping=true to copy from shipping.")
        .Produces<CheckoutSessionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Select shipping method
        group.MapPost("/{sessionId:guid}/shipping-method", async (
            Guid sessionId,
            SelectShippingMethodRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new SelectShippingMethodCommand(
                sessionId,
                request.ShippingMethod,
                request.ShippingCost,
                request.EstimatedDeliveryAt)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<CheckoutSessionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("SelectShippingMethod")
        .WithSummary("Select shipping method")
        .WithDescription("Selects a shipping method and sets the shipping cost.")
        .Produces<CheckoutSessionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Select payment method
        group.MapPost("/{sessionId:guid}/payment-method", async (
            Guid sessionId,
            SelectPaymentMethodRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new SelectPaymentMethodCommand(
                sessionId,
                request.PaymentMethod,
                request.PaymentGatewayId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<CheckoutSessionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("SelectPaymentMethod")
        .WithSummary("Select payment method")
        .WithDescription("Selects a payment method for the checkout.")
        .Produces<CheckoutSessionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Complete checkout
        group.MapPost("/{sessionId:guid}/complete", async (
            Guid sessionId,
            CompleteCheckoutRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CompleteCheckoutCommand(
                sessionId,
                request?.CustomerNotes)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<CheckoutSessionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("CompleteCheckout")
        .WithSummary("Complete checkout")
        .WithDescription("Completes the checkout session and creates an order.")
        .Produces<CheckoutSessionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
