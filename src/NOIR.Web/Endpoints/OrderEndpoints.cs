using NOIR.Application.Features.Orders.Commands.AddOrderNote;
using NOIR.Application.Features.Orders.Commands.CancelOrder;
using NOIR.Application.Features.Orders.Commands.CompleteOrder;
using NOIR.Application.Features.Orders.Commands.ConfirmOrder;
using NOIR.Application.Features.Orders.Commands.CreateOrder;
using NOIR.Application.Features.Orders.Commands.DeliverOrder;
using NOIR.Application.Features.Orders.Commands.DeleteOrderNote;
using NOIR.Application.Features.Orders.Commands.ManualCreateOrder;
using NOIR.Application.Features.Orders.Commands.ReturnOrder;
using NOIR.Application.Features.Orders.Commands.ShipOrder;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Queries.GetOrderById;
using NOIR.Application.Features.Orders.Queries.GetOrderNotes;
using NOIR.Application.Features.Orders.Queries.GetOrders;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Order API endpoints.
/// Provides CRUD operations for orders.
/// </summary>
public static class OrderEndpoints
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization();

        // Get all orders (paginated)
        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] OrderStatus? status,
            [FromQuery] string? customerEmail,
            [FromQuery] DateTimeOffset? fromDate,
            [FromQuery] DateTimeOffset? toDate,
            IMessageBus bus) =>
        {
            var query = new GetOrdersQuery(
                page ?? 1,
                pageSize ?? 20,
                status,
                customerEmail,
                fromDate,
                toDate);
            var result = await bus.InvokeAsync<Result<PagedResult<OrderSummaryDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersRead)
        .WithName("GetOrders")
        .WithSummary("Get paginated list of orders")
        .WithDescription("Returns orders with optional filtering by status, customer email, and date range.")
        .Produces<PagedResult<OrderSummaryDto>>(StatusCodes.Status200OK);

        // Get order by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetOrderByIdQuery(id);
            var result = await bus.InvokeAsync<Result<OrderDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersRead)
        .WithName("GetOrderById")
        .WithSummary("Get order by ID")
        .WithDescription("Returns full order details including items.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create order
        group.MapPost("/", async (
            CreateOrderCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<OrderDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("CreateOrder")
        .WithSummary("Create a new order")
        .WithDescription("Creates a new order with items, addresses, and customer information.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Manual create order (admin)
        group.MapPost("/manual", async (
            ManualCreateOrderCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<OrderDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("ManualCreateOrder")
        .WithSummary("Manually create a new order")
        .WithDescription("Create an order without going through checkout flow. For admin use.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Confirm order
        group.MapPost("/{id:guid}/confirm", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ConfirmOrderCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<OrderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("ConfirmOrder")
        .WithSummary("Confirm an order")
        .WithDescription("Confirms an order after payment is received.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Ship order
        group.MapPost("/{id:guid}/ship", async (
            Guid id,
            [FromBody] ShipOrderRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ShipOrderCommand(id, request.TrackingNumber, request.ShippingCarrier)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<OrderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("ShipOrder")
        .WithSummary("Ship an order")
        .WithDescription("Marks an order as shipped with tracking information.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Deliver order
        group.MapPost("/{id:guid}/deliver", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeliverOrderCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<OrderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("DeliverOrder")
        .WithSummary("Mark order as delivered")
        .WithDescription("Marks an order as delivered to the customer.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Complete order
        group.MapPost("/{id:guid}/complete", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CompleteOrderCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<OrderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("CompleteOrder")
        .WithSummary("Complete an order")
        .WithDescription("Marks an order as completed after successful delivery.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Return order
        group.MapPost("/{id:guid}/return", async (
            Guid id,
            [FromBody] ReturnOrderRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ReturnOrderCommand(id, request?.Reason) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<OrderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersManage)
        .WithName("ReturnOrder")
        .WithSummary("Return an order")
        .WithDescription("Marks an order as returned and releases inventory back to stock.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Cancel order
        group.MapPost("/{id:guid}/cancel", async (
            Guid id,
            [FromBody] CancelOrderRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CancelOrderCommand(id, request?.Reason) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<OrderDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("CancelOrder")
        .WithSummary("Cancel an order")
        .WithDescription("Cancels an order with an optional reason.")
        .Produces<OrderDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get order notes
        group.MapGet("/{orderId:guid}/notes", async (Guid orderId, IMessageBus bus) =>
        {
            var query = new GetOrderNotesQuery(orderId);
            var result = await bus.InvokeAsync<Result<IReadOnlyList<OrderNoteDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersRead)
        .WithName("GetOrderNotes")
        .WithSummary("Get notes for an order")
        .WithDescription("Returns all internal staff notes for an order, ordered by newest first.")
        .Produces<IReadOnlyList<OrderNoteDto>>(StatusCodes.Status200OK);

        // Add order note
        group.MapPost("/{orderId:guid}/notes", async (
            Guid orderId,
            [FromBody] AddOrderNoteRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddOrderNoteCommand(orderId, request.Content)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<OrderNoteDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("AddOrderNote")
        .WithSummary("Add a note to an order")
        .WithDescription("Adds an internal staff note to an order.")
        .Produces<OrderNoteDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete order note
        group.MapDelete("/{orderId:guid}/notes/{noteId:guid}", async (
            Guid orderId,
            Guid noteId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteOrderNoteCommand(orderId, noteId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<OrderNoteDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.OrdersWrite)
        .WithName("DeleteOrderNote")
        .WithSummary("Delete an order note")
        .WithDescription("Permanently deletes an internal staff note from an order.")
        .Produces<OrderNoteDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}

/// <summary>
/// Request DTO for shipping an order.
/// </summary>
public sealed record ShipOrderRequest(string TrackingNumber, string ShippingCarrier);

/// <summary>
/// Request DTO for cancelling an order.
/// </summary>
public sealed record CancelOrderRequest(string? Reason);

/// <summary>
/// Request DTO for returning an order.
/// </summary>
public sealed record ReturnOrderRequest(string? Reason);

/// <summary>
/// Request DTO for adding an order note.
/// </summary>
public sealed record AddOrderNoteRequest(string Content);
