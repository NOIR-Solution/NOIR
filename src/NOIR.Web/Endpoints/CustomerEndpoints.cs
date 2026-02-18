using NOIR.Application.Features.Customers.Commands.AddCustomerAddress;
using NOIR.Application.Features.Customers.Commands.AddLoyaltyPoints;
using NOIR.Application.Features.Customers.Commands.CreateCustomer;
using NOIR.Application.Features.Customers.Commands.DeleteCustomer;
using NOIR.Application.Features.Customers.Commands.DeleteCustomerAddress;
using NOIR.Application.Features.Customers.Commands.RedeemLoyaltyPoints;
using NOIR.Application.Features.Customers.Commands.UpdateCustomer;
using NOIR.Application.Features.Customers.Commands.UpdateCustomerAddress;
using NOIR.Application.Features.Customers.Commands.UpdateCustomerSegment;
using NOIR.Application.Features.Customers.DTOs;
using NOIR.Application.Features.Customers.Queries.GetCustomerById;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Customers.Queries.GetCustomerOrders;
using NOIR.Application.Features.Customers.Queries.GetCustomers;
using NOIR.Application.Features.Customers.Queries.GetCustomerStats;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Customer Management API endpoints.
/// Provides CRUD operations, loyalty points, and address management.
/// </summary>
public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .RequireAuthorization();

        // Get all customers (paginated)
        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] CustomerSegment? segment,
            [FromQuery] CustomerTier? tier,
            [FromQuery] bool? isActive,
            [FromQuery] string? sortBy,
            [FromQuery] bool? sortDescending,
            IMessageBus bus) =>
        {
            var query = new GetCustomersQuery(
                page ?? 1,
                pageSize ?? 20,
                search,
                segment,
                tier,
                isActive,
                sortBy,
                sortDescending ?? true);
            var result = await bus.InvokeAsync<Result<PagedResult<CustomerSummaryDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersRead)
        .WithName("GetCustomers")
        .WithSummary("Get paginated list of customers")
        .WithDescription("Returns customers with optional filtering by search, segment, tier, and active status.")
        .Produces<PagedResult<CustomerSummaryDto>>(StatusCodes.Status200OK);

        // Get customer by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetCustomerByIdQuery(id);
            var result = await bus.InvokeAsync<Result<CustomerDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersRead)
        .WithName("GetCustomerById")
        .WithSummary("Get customer by ID")
        .WithDescription("Returns full customer details including addresses.")
        .Produces<CustomerDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get customer stats
        group.MapGet("/stats", async (
            [FromQuery] int? topSpendersCount,
            IMessageBus bus) =>
        {
            var query = new GetCustomerStatsQuery(topSpendersCount ?? 10);
            var result = await bus.InvokeAsync<Result<CustomerStatsDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersRead)
        .WithName("GetCustomerStats")
        .WithSummary("Get customer statistics")
        .WithDescription("Returns segment distribution, tier distribution, and top spenders.")
        .Produces<CustomerStatsDto>(StatusCodes.Status200OK);

        // Get customer orders
        group.MapGet("/{id:guid}/orders", async (
            Guid id,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetCustomerOrdersQuery(id, page ?? 1, pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PagedResult<OrderSummaryDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersRead)
        .WithName("GetCustomerOrders")
        .WithSummary("Get customer order history")
        .WithDescription("Returns paginated order history for a specific customer.")
        .Produces<PagedResult<OrderSummaryDto>>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create customer
        group.MapPost("/", async (
            CreateCustomerCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { AuditUserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<CustomerDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersCreate)
        .WithName("CreateCustomer")
        .WithSummary("Create a new customer")
        .WithDescription("Creates a new customer with basic information.")
        .Produces<CustomerDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update customer
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCustomerCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { Id = id, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<CustomerDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersUpdate)
        .WithName("UpdateCustomer")
        .WithSummary("Update a customer")
        .WithDescription("Updates customer profile, tags, and notes.")
        .Produces<CustomerDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete customer
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteCustomerCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<CustomerDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersDelete)
        .WithName("DeleteCustomer")
        .WithSummary("Delete a customer")
        .WithDescription("Soft-deletes a customer.")
        .Produces<CustomerDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update customer segment
        group.MapPut("/{id:guid}/segment", async (
            Guid id,
            UpdateCustomerSegmentRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateCustomerSegmentCommand(id, request.Segment) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<CustomerDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersManage)
        .WithName("UpdateCustomerSegment")
        .WithSummary("Update customer segment")
        .WithDescription("Manually override a customer's segment.")
        .Produces<CustomerDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Add loyalty points
        group.MapPost("/{id:guid}/loyalty/add", async (
            Guid id,
            AddLoyaltyPointsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new AddLoyaltyPointsCommand(id, request.Points, request.Reason) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<CustomerDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersManage)
        .WithName("AddLoyaltyPoints")
        .WithSummary("Add loyalty points")
        .WithDescription("Adds loyalty points to a customer.")
        .Produces<CustomerDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Redeem loyalty points
        group.MapPost("/{id:guid}/loyalty/redeem", async (
            Guid id,
            RedeemLoyaltyPointsRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new RedeemLoyaltyPointsCommand(id, request.Points, request.Reason) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<CustomerDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersManage)
        .WithName("RedeemLoyaltyPoints")
        .WithSummary("Redeem loyalty points")
        .WithDescription("Redeems loyalty points from a customer.")
        .Produces<CustomerDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Add customer address
        group.MapPost("/{id:guid}/addresses", async (
            Guid id,
            AddCustomerAddressCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { CustomerId = id, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<CustomerAddressDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersUpdate)
        .WithName("AddCustomerAddress")
        .WithSummary("Add customer address")
        .WithDescription("Adds an address to a customer.")
        .Produces<CustomerAddressDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Update customer address
        group.MapPut("/{id:guid}/addresses/{addressId:guid}", async (
            Guid id,
            Guid addressId,
            UpdateCustomerAddressCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { CustomerId = id, AddressId = addressId, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<CustomerAddressDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersUpdate)
        .WithName("UpdateCustomerAddress")
        .WithSummary("Update customer address")
        .WithDescription("Updates an existing customer address.")
        .Produces<CustomerAddressDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete customer address
        group.MapDelete("/{id:guid}/addresses/{addressId:guid}", async (
            Guid id,
            Guid addressId,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteCustomerAddressCommand(id, addressId) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<CustomerAddressDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.CustomersUpdate)
        .WithName("DeleteCustomerAddress")
        .WithSummary("Delete customer address")
        .WithDescription("Removes an address from a customer.")
        .Produces<CustomerAddressDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}

/// <summary>
/// Request DTO for updating customer segment.
/// </summary>
public sealed record UpdateCustomerSegmentRequest(CustomerSegment Segment);

/// <summary>
/// Request DTO for adding loyalty points.
/// </summary>
public sealed record AddLoyaltyPointsRequest(int Points, string? Reason = null);

/// <summary>
/// Request DTO for redeeming loyalty points.
/// </summary>
public sealed record RedeemLoyaltyPointsRequest(int Points, string? Reason = null);
