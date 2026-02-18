using NOIR.Application.Features.Promotions.Commands.ActivatePromotion;
using NOIR.Application.Features.Promotions.Commands.ApplyPromotion;
using NOIR.Application.Features.Promotions.Commands.CreatePromotion;
using NOIR.Application.Features.Promotions.Commands.DeactivatePromotion;
using NOIR.Application.Features.Promotions.Commands.DeletePromotion;
using NOIR.Application.Features.Promotions.Commands.UpdatePromotion;
using NOIR.Application.Features.Promotions.DTOs;
using NOIR.Application.Features.Promotions.Queries.GetPromotionById;
using NOIR.Application.Features.Promotions.Queries.GetPromotions;
using NOIR.Application.Features.Promotions.Queries.ValidatePromoCode;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Promotion API endpoints.
/// Provides CRUD operations and lifecycle management for promotions and vouchers.
/// </summary>
public static class PromotionEndpoints
{
    public static void MapPromotionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/promotions")
            .WithTags("Promotions")
            .RequireAuthorization();

        // Get all promotions (paginated)
        group.MapGet("/", async (
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromQuery] string? search,
            [FromQuery] PromotionStatus? status,
            [FromQuery] PromotionType? promotionType,
            [FromQuery] DateTimeOffset? fromDate,
            [FromQuery] DateTimeOffset? toDate,
            IMessageBus bus) =>
        {
            var query = new GetPromotionsQuery(
                page ?? 1,
                pageSize ?? 20,
                search,
                status,
                promotionType,
                fromDate,
                toDate);
            var result = await bus.InvokeAsync<Result<PagedResult<PromotionDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PromotionsRead)
        .WithName("GetPromotions")
        .WithSummary("Get paginated list of promotions")
        .WithDescription("Returns promotions with optional filtering by search, status, type, and date range.")
        .Produces<PagedResult<PromotionDto>>(StatusCodes.Status200OK);

        // Get promotion by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetPromotionByIdQuery(id);
            var result = await bus.InvokeAsync<Result<PromotionDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PromotionsRead)
        .WithName("GetPromotionById")
        .WithSummary("Get promotion by ID")
        .WithDescription("Returns full promotion details including product/category targeting and usage stats.")
        .Produces<PromotionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create promotion
        group.MapPost("/", async (
            CreatePromotionCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<PromotionDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PromotionsWrite)
        .WithName("CreatePromotion")
        .WithSummary("Create a new promotion")
        .WithDescription("Creates a new promotion with discount rules, targeting, and usage limits.")
        .Produces<PromotionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        // Update promotion
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePromotionCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { Id = id, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<PromotionDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PromotionsWrite)
        .WithName("UpdatePromotion")
        .WithSummary("Update a promotion")
        .WithDescription("Updates promotion details, discount rules, and targeting.")
        .Produces<PromotionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Delete promotion (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeletePromotionCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PromotionsDelete)
        .WithName("DeletePromotion")
        .WithSummary("Delete a promotion")
        .WithDescription("Soft deletes a promotion.")
        .Produces<bool>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Activate promotion
        group.MapPost("/{id:guid}/activate", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new ActivatePromotionCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<PromotionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PromotionsWrite)
        .WithName("ActivatePromotion")
        .WithSummary("Activate a promotion")
        .WithDescription("Activates a promotion, making it available for use.")
        .Produces<PromotionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Deactivate promotion
        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeactivatePromotionCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<PromotionDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PromotionsWrite)
        .WithName("DeactivatePromotion")
        .WithSummary("Deactivate a promotion")
        .WithDescription("Deactivates a promotion, preventing further use.")
        .Produces<PromotionDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Apply promotion code
        group.MapPost("/apply", async (
            ApplyPromotionCommand command,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var auditableCommand = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<PromotionUsageDto>>(auditableCommand);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PromotionsRead)
        .WithName("ApplyPromotion")
        .WithSummary("Apply a promotion code")
        .WithDescription("Validates and applies a promotion code to an order, recording the usage.")
        .Produces<PromotionUsageDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Validate promotion code
        group.MapGet("/validate/{code}", async (
            string code,
            [FromQuery] decimal orderTotal,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var query = new ValidatePromoCodeQuery(code, orderTotal, currentUser.UserId);
            var result = await bus.InvokeAsync<Result<PromoCodeValidationDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.PromotionsRead)
        .WithName("ValidatePromoCode")
        .WithSummary("Validate a promotion code")
        .WithDescription("Checks if a promotion code is valid and calculates the potential discount amount.")
        .Produces<PromoCodeValidationDto>(StatusCodes.Status200OK);
    }
}
