using NOIR.Application.Features.Inventory.Commands.CancelInventoryReceipt;
using NOIR.Application.Features.Inventory.Commands.ConfirmInventoryReceipt;
using NOIR.Application.Features.Inventory.Commands.CreateInventoryReceipt;
using NOIR.Application.Features.Inventory.Commands.CreateStockMovement;
using NOIR.Application.Features.Inventory.DTOs;
using NOIR.Application.Features.Inventory.Queries.GetInventoryReceiptById;
using NOIR.Application.Features.Inventory.Queries.GetInventoryReceipts;
using NOIR.Application.Features.Inventory.Queries.GetStockHistory;
using NOIR.Domain.Enums;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Inventory API endpoints.
/// Provides stock movement and inventory receipt operations.
/// </summary>
public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory")
            .WithTags("Inventory")
            .RequireAuthorization();

        // Get stock history for a variant
        group.MapGet("/products/{productId:guid}/variants/{variantId:guid}/history", async (
            Guid productId,
            Guid variantId,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetStockHistoryQuery(productId, variantId, page ?? 1, pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<PagedResult<InventoryMovementDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InventoryRead)
        .WithName("GetStockHistory")
        .WithSummary("Get stock movement history")
        .WithDescription("Returns paginated stock movement history for a product variant.")
        .Produces<PagedResult<InventoryMovementDto>>(StatusCodes.Status200OK);

        // Create manual stock movement
        group.MapPost("/movements", async (
            CreateStockMovementCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<InventoryMovementDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InventoryWrite)
        .WithName("CreateStockMovement")
        .WithSummary("Create manual stock movement")
        .WithDescription("Creates a manual stock movement (StockIn, StockOut, or Adjustment).")
        .Produces<InventoryMovementDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // --- Inventory Receipts ---

        // Get all receipts (paginated)
        group.MapGet("/receipts", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] InventoryReceiptType? type,
            [FromQuery] InventoryReceiptStatus? status,
            IMessageBus bus) =>
        {
            var query = new GetInventoryReceiptsQuery(page ?? 1, pageSize ?? 20, type, status);
            var result = await bus.InvokeAsync<Result<PagedResult<InventoryReceiptSummaryDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InventoryRead)
        .WithName("GetInventoryReceipts")
        .WithSummary("Get paginated inventory receipts")
        .WithDescription("Returns inventory receipts with optional filtering by type and status.")
        .Produces<PagedResult<InventoryReceiptSummaryDto>>(StatusCodes.Status200OK);

        // Get receipt by ID
        group.MapGet("/receipts/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetInventoryReceiptByIdQuery(id);
            var result = await bus.InvokeAsync<Result<InventoryReceiptDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InventoryRead)
        .WithName("GetInventoryReceiptById")
        .WithSummary("Get inventory receipt by ID")
        .WithDescription("Returns full inventory receipt details including items.")
        .Produces<InventoryReceiptDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create receipt
        group.MapPost("/receipts", async (
            CreateInventoryReceiptCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<InventoryReceiptDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InventoryWrite)
        .WithName("CreateInventoryReceipt")
        .WithSummary("Create inventory receipt")
        .WithDescription("Creates a new inventory receipt (draft) with items.")
        .Produces<InventoryReceiptDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Confirm receipt
        group.MapPost("/receipts/{id:guid}/confirm", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ConfirmInventoryReceiptCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<InventoryReceiptDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InventoryManage)
        .WithName("ConfirmInventoryReceipt")
        .WithSummary("Confirm inventory receipt")
        .WithDescription("Confirms an inventory receipt and adjusts stock for all items.")
        .Produces<InventoryReceiptDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Cancel receipt
        group.MapPost("/receipts/{id:guid}/cancel", async (
            Guid id,
            [FromBody] CancelReceiptRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CancelInventoryReceiptCommand(id, request?.Reason) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<InventoryReceiptDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.InventoryManage)
        .WithName("CancelInventoryReceipt")
        .WithSummary("Cancel inventory receipt")
        .WithDescription("Cancels a draft inventory receipt.")
        .Produces<InventoryReceiptDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}

/// <summary>
/// Request DTO for cancelling an inventory receipt.
/// </summary>
public sealed record CancelReceiptRequest(string? Reason);
